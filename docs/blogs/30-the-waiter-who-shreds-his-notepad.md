# The Waiter Who Shreds His Notepad

*Published 2026-07-18*

There's a café where the waiter forgets you between sentences.

You order a coffee. He writes it on his notepad, walks to the kitchen, files the order in a big cabinet, and shreds the notepad. When he comes back, he has no idea who you are. You ask for milk. He doesn't know you ordered a coffee. So he walks back to the cabinet, finds your file, reads it, writes "milk" on a new page, files it, and shreds the notepad again.

Every sentence you say costs him a trip to the cabinet, because your order is never allowed to stay in his head.

The waiter isn't broken. He is following the café's rules, and almost every app on your phone works the same way.

The rule is called *stateless*. It means the server is not allowed to remember anything between requests. The rule made sense long ago, when servers crashed often and memory was expensive, so servers were built to keep nothing. Every click starts from zero. The server looks the user up, fetches their data from the database, answers, and forgets.

A café run this way needs a lot of extra staff. Someone has to carry messages between the waiter and the cabinet (the API layer). Trips to the cabinet are slow, so someone keeps sticky notes near the kitchen with the most common answers (the cache). Someone else has to guess when a sticky note has gone out of date, which is famously hard. And you get a photocopy of your order taped to your table, so you can see it without asking (the app on your phone). The photocopy goes out of date too, so the app keeps asking the server, over and over: *did anything change? did anything change?*

Now count the copies of one cup of coffee: a row in the cabinet, a form the runner carries, a sticky note, a photocopy on your table and a pencil mark on the photocopy. That is five copies of one fact, and most of the work in the café is keeping those five copies in agreement.

In an Ikon app, the coffee is one line of code, a `Reactive<List<Order>>`. It is held in the memory of a small server that is yours alone, and that server keeps running while you use the app. When the order changes, the server sends the change and your screen updates without the app having to ask. Nobody carries messages, keeps sticky notes or tapes a photocopy to your table. The waiter remembers.

For twenty years, the forgetful café was only wasteful. Then the café hired a genius.

The new waiter is an AI, and it charges by the word. It is brilliant, but under the old rules it forgets you between sentences like everyone else. So before every reply, someone has to read it the whole story of your meal, out loud, at full price. In software, that means every request to the model sends the whole conversation again, and you pay for all of it each time. You pay to explain the coffee. Then you pay to explain the coffee *and* the milk. The expensive part is not the thinking. It is paying again and again to give the model back the memory it was not allowed to keep.

Then comes a task the stateless rule cannot handle at all. You ask the AI to plan a dinner party: call the guests, compare menus, book a room, and check back when the caterer replies. That is an hour of work with loose ends to keep track of, not a question with one answer. How do you give an hour of work to a worker who forgets everything every thirty seconds? The usual answer is to cut the hour into small pieces, save everything to storage between pieces, and hope each piece can carry on from the saved record alone. Whole frameworks exist to do exactly this. They exist because the job needs memory and the servers are not allowed to keep any.

In Ikon, the dinner party is a loop in your app, and the loose ends are variables in the server's memory. The waiter keeps your evening in his head, because that is what heads are for.

That leaves one fair question: who pays a waiter to stand around all night in an empty café? Nobody. When the last guest leaves, he writes the evening into a small notebook and goes home. He costs nothing while he is away. When you walk back in, he is already coming out of the back room, notebook open, saying your name.

Stateless servers were a workaround for expensive memory and unreliable machines, and both problems are long gone. What is left is a café full of runners and sticky notes serving a genius who charges by the word, while the cheapest thing in the whole building is the notepad nobody was allowed to keep.
