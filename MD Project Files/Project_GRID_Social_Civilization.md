# Project GRID - Social Civilization (3.3)

**Implemented and verified - 2026-10-20**

## Scope and configuration

Phase 3.3 adds deterministic, bounded civic interactions for explicitly opted-in persistent Agent and Special residents. `ConfigureSocial(programId, preference)` is a trusted engine API; it does not create agents or supply production login/authorization. Host Application residents and Legacy demo citizens cannot opt in. Their observed availability, energy and resource semantics remain unchanged.

Preferences are Cooperative, Competitive and Reserved. Configuration preserves existing memories, relationships and credits. ConfigureRoutine records occupation changes for opted-in residents, including their initial occupation (or Unassigned) at opt-in. This is configured civic occupation history, not a record of arbitrary Windows process jobs.

## Social rounds and experiences

A social round occurs after ten simulated seconds. At most one round runs per simulation tick; oversized steps discard excess round time. Pause freezes social time, saves retain partial round progress, and restoration starts paused without offline catch-up.

Policy uses projected post-behavior locations and actions. Only residents in the same district who are Idle, Resting or OffDuty and have no multi-district route can interact. Working, Moving, Unknown and WaitingForResources residents cannot. No interaction teleports a resident, changes work progress or consumes host energy.

Residents are ordered by stable ID, with the starting position rotating each round. Each resident participates in at most one pair per round. Partners must share a preference; Cooperative residents prefer higher remembered affinity, Competitive residents prefer lower affinity, and stable IDs break ties. Reserved residents observe without pairing. An unmatched available resident seeks a partner.

- Cooperation consumes one district civic token for the pair, gives each resident one civic credit and increases each directed affinity by two.
- Competition consumes one token, gives the first resident one credit, records a win and loss, and lowers both directed affinities by one. Rotation changes who gets first choice; it does not guarantee equal outcomes in every population.
- An empty pool records ResourcesUnavailable for both residents, changes their last social goal to WaitForCivicResources and leaves affinity unchanged.

Memories contain tick, peer, district and outcome. Last social goals (SeekPartner, Observe, Share, Compete or WaitForCivicResources) describe the last social decision and are separate from current work/movement goals. Memories affect later partner selection through remembered affinity; they are structured experiences, not natural-language recollections.

## Fictional civic scarcity and limits

Civic tokens are explicitly simulated resources, independent of measured CPU, RAM, GPU, storage, network and application energy. Default district pools start with four tokens, capacity four and recovery of one per round before interactions. Trusted `ConfigureCivicPool(districtId, capacity, recoveryPerRound)` accepts capacity 1..64 and recovery 0..capacity. Reducing capacity clamps stock; increasing capacity does not instantly refill it.

Limits: 128 opted-in residents, 256 district pools, 16 directed relationships and 16 recent memories per resident, eight occupation-history entries, credits 0..100 and affinity -100..100. Old memories/history are dropped; relationships retain the most recently used peers, with stable-ID tie breaking. These are bounded local social mechanics, not a complete economy.

## State ownership, storage and history

The engine stages social decisions before committing a tick and preflights resident revisions alongside behavior changes. Cancellation or validation/overflow failure leaves clock, memberships, pools and memories unchanged. Public snapshots expose owned read-only social collections; restore copies and validates supplied collections.

SQLite schema v8 adds WorldSocialState, one world-linked JSON row with a two-million-character bound. Existing migrations retain their checksums. Older worlds without a social row load with empty social state and receive the row on the next save. Saves and backup recovery retain preferences, relationships, memories, occupations, pools, credits and partial round progress. Existing validation rejects invalid peers, resident kinds, values, dates/ticks and bounds. Ordinary application residents are not persisted as fictional social actors.

SocialConfigured, CivicPoolConfigured, SocialInteraction and net CivicResources changes use the existing bounded event journal and transactional history save. Pair events include structured participant/district/outcome data. A round that replenishes and spends the same token count has interaction events but no net-stock event. Events become durable on save; pending journal limits and crash-loss windows still apply.

## Terminal

- `social`: opted-in resident table and simulated district token pools.
- `social <id|name>`: relationships, recent memories and occupation history; ambiguous names require a full ID.
- `inspect program <id|name>`: includes social details when opted in.
- `history kind=SocialInteraction`: queries saved pair events in persistent sessions.

These views are read-only. A normal world may show an empty social roster: automatic agent creation and interactive admin social configuration are not implemented. The `city` description now correctly distinguishes implemented district simulation from planned 3D rendering.

## Completion verification and remaining work

**336 tests passed:** 14 Core, 144 Simulation, 86 Persistence and 92 Terminal. Each test project ran with `dotnet test <project> -c Verification --no-restore`. Coverage includes cooperation, competition, preference/affinity selection, scarcity/recovery, bounded state, occupation changes, working/moving/separate-district exclusion, deterministic replay, timing, cancellation/overflow, invalid restore, host-application exclusion, save/load/backup recovery, populated v7 upgrade and read-only terminal output.

A real terminal smoke using a new temporary world passed `social`, `city`, orderly save and reopen. The user's default world and existing running Release terminal were not used. Verification builds are separate from the normal launcher target; rebuild/restart that target to use the new commands.

Remaining: Phase 3.4 sustained autonomy/performance testing; real agent communication, delegation and LLM integration; production identity/permissions; terminal admin configuration; automatic agent population; richer economies and 3D presentation. No real host tasks or messages are executed by civic interactions. Prior dated milestone notes remain historical evidence.
