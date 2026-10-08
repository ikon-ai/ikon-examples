# The Database That Forgets

*Published 2026-04-20*

Every night while you sleep, your brain throws things away.

This is meant literally. Neuroscientists call it synaptic pruning: during deep sleep, the brain weakens or removes the connections that were not important that day. The memories that survive are the ones that were reinforced, linked to other memories, or used. The rest fade before morning.

Forgetting is part of how memory works. A brain that kept everything would have trouble finding anything useful, because every memory it looked for would be buried in trivia.

A database works differently, whether it is Postgres, Mongo, Neo4j or a vector store. It keeps everything you ever inserted, forever, exactly as it was on the day you put it in, and it has no way to forget on its own. To remove data, you write `DELETE` statements, set TTLs or put an LRU cache in front. These tools are crude. Each of them removes data by a fixed rule (a command, an expiry time or a size limit), so none of them can decide *what* is worth removing.

## Every database is an accumulator

In fifty years of database research, we have built one kind of system. A relational database accumulates rows. A document store accumulates documents. A graph database accumulates nodes and edges. A vector database accumulates embeddings. Caches remove entries, but a cache is only a thin layer in front of the real data. The main store, where the data actually lives, only ever grows.

For most uses this is right. Transactions, logs and user records should be kept. A bank statement is supposed to remember every payment forever, and a database that "forgets" your balance has a bug.

Memory systems for AI work more like brains than like banks. Their job is to let an agent recall what matters from a growing pile of messages, documents and observations. When you feed more into them, the useful information gets harder to find. Twenty documents about the same topic don't make the system twenty times better informed. They make its picture of the topic vaguer. A fact stated for the third time adds noise instead of making the fact more certain. Past a point, more data blurs the structure the system has built.

## The stress test

We built a world-model library called Kairon that reads documents and builds its own structure from them, with no fixed schema and no hand-written ontology. It takes chunks of text as input and produces a graph that it organizes itself.

The first version only added data, like every other system. Each new document added nodes, edges and clusters, and the graph kept growing.

We ran it on a hundred documents and it looked healthy. We ran it on five hundred and the quality started slipping. We ran it on a thousand and the benchmark scores dropped sharply. The more it read, the less it understood, like a student cramming the night before an exam.

## Teaching the graph to forget

The fix was to give the system something like a night's sleep. After ingestion, Kairon now runs a series of passes that decide what to keep.

The first is a subtraction pass. For every node, it checks whether the node adds to the structure: whether anything cites it, whether it connects ideas that would otherwise be unconnected, and whether it is a weaker duplicate of another node. It scores each node on these checks. The score does not depend on when the node was written or how often it was read. Then the pass removes the nodes that contribute least.

A fading pass followed. Nodes that new documents no longer reinforce lose a little weight, so a claim that was strong yesterday is weaker today unless something supports it again. A merge pass combines near-duplicate nodes into one stronger node instead of deleting one of them. Last came depth-over-breadth scoring, which raises the score of nodes that chains of reasoning are built on and lowers the score of nodes that have no connections.

The thousand-document version now scores higher than the hundred-document version, because it threw most of the thousand away.

## Forgetting on purpose

Every database ever shipped keeps whatever you put into it. Data stays until you remove it with a `DELETE`, and deciding what to remove is entirely your job. The database is designed to keep everything.

Kairon decides for itself what is worth keeping, based on the structure it is building. Its passes run after ingestion, much as sleep follows a day, and remove what does not contribute to that structure.

No off-the-shelf product does this, including Postgres, Neo4j, Pinecone and Mongo. The LLM memory libraries released this year mostly overwrite a fact when a newer fact contradicts it, which is a form of `UPDATE` rather than forgetting. The closest comparison is not a database at all but your brain between one and four in the morning, throwing away everything that did not matter that day.

Kairon is a database that gets smaller as it gets better at answering questions.
