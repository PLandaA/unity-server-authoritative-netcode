# Server-Authoritative Netcode Prototype (Unity)

A minimal server-authoritative multiplayer prototype built from scratch in Unity, with no networking framework. One server and two clients run inside a single scene, connected by message queues with artificial latency, in the spirit of Gabriel Gambetta's *Fast-Paced Multiplayer* demo.

The goal is to implement and be able to explain, from first principles, the core techniques behind responsive networked gameplay: **client-side prediction**, **server reconciliation** and **entity interpolation**.

This is **Week 1 of 3**: the pure authoritative model, with no prediction yet. It is intentionally laggy, so the problem the next weeks solve is visible and measurable.

## What it does

- One `Server` and two `Client`s live in the same scene and only talk through `LagQueue<T>`, a message queue that delivers each message after a configurable delay.
- Clients send **intentions** (`InputMessage`), never positions. The server simulates at a fixed tick and publishes the authoritative state (`StateMessage`) to every client.
- Each client draws only what it has received. With 100 ms per leg, a key press takes ~200 ms to show on screen.
- A latency slider changes the delay live, and a HUD shows the server's position next to each client's view of it, so the lag is visible as a number, not just a feeling.

[GIF: moving cube A with latency at 100 ms]

## How to run

- Unity [version].
- Open `Assets/Scenes/[SceneName]`, press Play.
- **WASD** moves client A, **arrow keys** move client B. The slider sets one-way latency in ms.
- Tests: `Window → General → Test Runner → EditMode`. [N] tests cover `LagQueue`, `Server` and `Client` without opening a scene.

## Architecture

```
Client A ──InputMessage──► LagQueue ──► Server ──StateMessage──► LagQueue ──► Client A
Client B ──InputMessage──► LagQueue ──►        ──StateMessage──► LagQueue ──► Client B

Server tick (every 50 ms):  Drain  →  Simulate  →  Publish
```

- **`LagQueue<T>`** — a FIFO of `(deliveryTime, message)`. `Send` stamps `now + latency`; `TryReceive` only looks at the front and delivers it when its time has come. O(1), no allocations, testable with a fake clock.
- **`Server`** — owns the only authoritative state. `Drain` empties every input queue and keeps the direction received this tick; `Simulate` integrates `position += direction * speed * dt`; `Publish` sends the same snapshot to every client. `Simulate` knows nothing about the network.
- **`Client`** — sends its direction every tick and keeps the latest snapshot it has received. It does not move anything on its own.
- **`Orchestrator`** — the only `MonoBehaviour`. Owns the clock (integer ms), runs the ticks, reads the keyboard and draws the cubes. No networking logic lives here.

## Design decisions

- **Time is integer milliseconds.** Avoids float drift in delivery times and makes tests exact.
- **The channel preserves order.** `TryReceive` only inspects the front of the queue, so a message cannot overtake an older one. If latency is lowered mid-game, new messages wait behind old ones and arrive later than configured. Real UDP gives no such guarantee; that is what sequence numbers will handle in Week 2.
- **Messages and state are `struct`s.** A snapshot in flight must never share memory with the server's live state; passing a `struct` through `Send` copies it, so a later tick cannot alter a message that has not arrived yet. With real serialization this problem disappears on its own; it exists precisely because the network is simulated in memory.
- **Server state is private.** The outside world gets a read-only copy. Nothing but `Simulate` can write a position; that is what "authoritative" means in code.
- **Fixed tick, not per-message.** Same `dt` every time makes the simulation deterministic: the same inputs always produce the same positions. That property is what lets a client re-simulate the server's work in Week 2. The cost is up to one tick of extra latency.
- **Multiple inputs in one tick: last one wins.** Provisional. With numbered inputs each one will map to exactly one tick.
- **An input whose `clientId` does not match the queue it arrived on is ignored and logged.** The server never trusts a message to say who sent it.
- **Clients tick before the server.** A message sent this tick is processed this tick; with zero latency the reverse order would add one tick.
- **Ticks use `nextTickMs`, not the real clock.** Every tick happens at an exact multiple of 50 ms regardless of frame rate, so delivery times are regular and a long frame can run several ticks correctly.

## What it feels like (measurements)

Press-to-screen delay for client A, measured in the Editor at ~[fps] fps:

| One-way latency | Measured | Expected (`2 × latency + 0..tick`) |
|---|---|---|
| 0 ms | [62–92] ms | 50–100 ms |
| 100 ms | [199–244] ms | 200–250 ms |
| [300] ms | [x–y] ms | [600–650] ms |

The `0..tick` term is the wait until the next tick after the key press. At zero latency there is still a full tick of delay: the client drains its queue before the server publishes in the same tick, so the snapshot is seen one tick later. With real latency that cost disappears because the snapshot is already due when the client ticks.

[If applicable: the first measurement at "300 ms" came out ~200 ms higher than predicted. Working the formula backwards pointed at 400 ms, which turned out to be the slider's actual value.]

## Known limitations / Next

- No client-side prediction: the player feels the full round-trip on their own character.
- No server reconciliation.
- No interpolation of the remote player: it moves in 50 ms steps.
- No packet loss, reordering or sequence numbers.

**Week 2** — client-side prediction and server reconciliation with numbered inputs.
**Week 3** — interpolation of remote players; optional real UDP transport with LiteNetLib.

## References

- Gabriel Gambetta, *Fast-Paced Multiplayer, Part I: Client-Server Game Architecture* — https://gabrielgambetta.com/client-server-game-architecture.html
- Glenn Fiedler, *What Every Programmer Needs To Know About Game Networking* — https://gafferongames.com/categories/game-networking/
- Tom Weiland, *Riptide Multiplayer Tutorials*, Parts 1–2 — https://www.youtube.com/playlist?list=PLXkn83W0Qkfn3qF7NU4OEtVwOD6U2wphJ
