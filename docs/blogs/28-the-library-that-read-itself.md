# The Library That Read Itself

*Published 2026-04-17*

A developer writes a documentation site with twenty-six hand-written sections. Each section is refined in an optimization loop until a classifier can send any user question to the right page. The system works. When a user asks "how do I animate a button on hover?", it picks the motion reference, builds the context from it and answers correctly. The weeks spent writing and refining the sections pay off.

Then someone asks: what if the documentation could organize itself?

## The limits of hand-written docs

The hand-written system, Oracle, works well. It uses a small, fast model to route each question to sections, joins the text of the selected guides, and generates an answer from it. It is simple, and it is exactly as good as the person who wrote the guides.

That is also its limit. It can only answer questions about topics someone chose to document, and only in the language the guides were written in. When the platform changes, someone has to update the guides. If the documentation is wrong, the answers are wrong, because the system has no source of truth other than the text a person wrote.

## Thirty-seven experiments

We built a system that reads the same raw documentation and works out its structure without help. Nobody writes a table of contents or individual sections. The system takes chunks of text as input and builds a knowledge graph, choosing the structure itself.

On the first benchmark it scored 0.42, against Oracle's 0.86.

Over thirty-seven iterations we tried many approaches. A monolithic compiler that read the whole knowledge graph and picked the relevant sections in a single call scored 0.71, then dropped to 0.27 when we gave it more context. A pipeline where each AI call has exactly one job scored 0.08 on the first try, because two of its stages were working against each other. With that conflict fixed, it scored 0.56. Adding a keyword index as a first, coarse filter raised it to 0.65. Adding a restructure pass, where the AI reads every entity at once and decides the section tree, raised it to 0.73.

Oracle stayed at 0.79.

## What the focused stages taught us

The monolithic approach failed because a single AI call had to do five things at once: understand the question, read the ontology, pick types, build a plan and write a rationale. Its results were unpredictable. Sometimes it worked, and when it did not, there was no way to tell which of the five jobs had gone wrong.

In the pipeline approach, one call extracts the intent of the question, parallel calls classify the sections, and a step with no AI in it assembles the context. It started with worse scores, but every failure could be traced to a cause. If the classifier scored a section at 0.58 when it should have been 0.85, the log shows the reason the classifier gave for that section. If the right section was missing from the tree, the restructure prompt needed to be more specific. Each problem had a specific cause and a specific fix.

Over time these small fixes added up. The monolith's score stopped improving at a certain point. The pipeline's score kept rising, because each new stage could be added without changing how the other stages behaved.

## The restructure pass

The problem that remained was that the knowledge graph came out differently every time. If the same documents were read in a different order, different clusters formed. A section about scroll areas might exist in one run and be missing in the next, and the classifier cannot pick a section that does not exist.

The fix was simple. After reading all the documents, the system asks the AI to look at everything it has collected and decide what the sections should be. The AI makes that decision once, with all the material in view, instead of the structure growing piece by piece as documents stream in.

Run-to-run variance dropped from ±0.15 to ±0.02. Every important topic now got its own section, because the AI created that section on purpose instead of relying on the streaming algorithm to group the material that way by chance.

## What the numbers mean

The self-organizing system now scores 0.73 against the hand-curated system's 0.79. On four of the eight test questions, the self-organizing system scores higher. It finds the specific passage inside a document that answers the question, while the hand-curated system returns a whole guide section, including the parts that don't apply.

The gap is six points. Behind the hand-curated system are weeks of human editing. The self-organizing system starts from raw documents and a pipeline of AI calls that each do one job.

The self-organizing system works in any language, on any topic and with any set of documents. Nobody has to write or maintain twenty-six guides, or decide what the sections should be. The AI reads the material, builds the structure and answers the questions.

The library read itself, and on this benchmark it came within six points of the version a person spent weeks on.
