# The Waiter Who Shreds His Notepad

There's a café where the waiter forgets you between sentences.

You order a coffee. He writes it on his notepad, walks to the kitchen, files the order in a big cabinet, and shreds the notepad. When he comes back, he has no idea who you are. You ask for milk. He doesn't know you ordered a coffee. So he walks back to the cabinet, finds your file, reads it, writes "milk" on a new page, files it, and shreds the notepad again.

Every sentence you say costs him a trip to the cabinet. Because the one place your order may never live is in his head.

Here's the strange part: the waiter isn't broken. He's following the rules. And this is how almost every app on your phone works.

The rule is called *stateless*. It means the server is not allowed to remember anything between requests. The rule made sense long ago: servers crashed often and memory was expensive, so we decided servers should keep nothing. Every click starts from zero. Look the user up, fetch their data from the database, answer, forget.

But a café run this way needs a lot of extra staff. Someone has to carry messages between the waiter and the cabinet — that's the API layer. Trips to the cabinet are slow, so someone keeps sticky notes near the kitchen with the most common answers — that's the cache. Someone else has to guess when a sticky note has gone stale, which is famously hard. And you get a photocopy of your order taped to your table, so you can see it without asking — that's the app on your phone. The photocopy drifts out of date too, so the app keeps asking the server, over and over: *did anything change? did anything change?*

Now count the copies of one cup of coffee. A row in the cabinet. A form the runner carries. A sticky note. A photocopy on your table. A pencil mark on the photocopy. Five copies of one fact — and most of the work in the café is keeping those five copies from disagreeing.

In an Ikon app, the coffee is one line of code — `Reactive<List<Order>>`. It lives in the memory of a small server that is yours alone. The server stays up. When the order changes, your screen updates by itself. No runner. No sticky notes. No photocopy. The waiter just remembers.

For twenty years, the forgetful café was only wasteful. Then the café hired a genius.

The new waiter is an AI, and it charges by the word. It's brilliant. But under the old rules it forgets you between sentences like everyone else. So before every reply, someone has to read it the whole story of your meal, out loud, at full price. You pay to explain the coffee. Then you pay to explain the coffee *and* the milk. The expensive part is not the thinking. The expensive part is buying back the memory, again and again.

Then comes the task that breaks the rule for good. You ask the AI to plan a dinner party. Call the guests. Compare menus. Book a room. Check back when the caterer replies. That's not a question with one answer. It's an hour of work with loose ends to keep in mind. How do you give an hour of work to a worker who forgets everything every thirty seconds? The industry's answer: cut the hour into tiny pieces, write everything down between pieces, and hope the next piece can pick up cold. There are whole frameworks built to do exactly this. They are all a quiet admission — the job needs memory, and our servers aren't allowed to have any.

In Ikon, the dinner party is just a loop in your app. The loose ends are variables. The waiter holds your evening in his head, because holding things in your head is what heads are for.

One fair question remains: who pays a waiter to stand around all night in an empty café? Nobody. When the last guest leaves, he writes the evening into a small notebook and goes home. He costs nothing while he's away. When you walk back in, he's already coming out of the back room, notebook open, saying your name.

Stateless was a workaround for expensive memory and unreliable machines. Both problems are long gone. What's left is a café full of runners and sticky notes, serving a genius who charges by the word — while the cheapest thing in the whole building is the notepad nobody was allowed to keep.
