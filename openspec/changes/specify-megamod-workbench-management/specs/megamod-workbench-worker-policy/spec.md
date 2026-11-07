# Workbench worker policy

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.
Origin: replacement. Specification only; no implementation or live acceptance.

## ADDED Requirements

### Requirement: A contextual worker policy governs the production list
The mod SHALL let the player apply the current production bill's worker restriction and allowed skill range to all supported bills of its workbench using a contextual Restrict workbench control.
Show the affected scope before applying it. Built-in state is unrestricted. Native named-worker,
any-worker, slave-only, mech-only and non-mech choices appear only where applicable. The initial
ordinary skill range is 0–20. Enabling captures the current bill's choices, applies them to the
existing list and makes future bill additions inherit the same policy. Editing these choices through
any bill while enabled updates the whole affected list. This is gameplay state, not a mod setting.
For ordinary named assignments, the stored range retains native applicability: it is not an added
named-worker range gate, but applies again to applicable non-named choices. Recipe requirements remain.
Do not derive the target bench from unrelated selection while a bill dialog remains open.
The most-skilled-workers capability adds its complete dynamic choice, Include away, Ignore if downed
and visible range to this same policy. Each recipe still evaluates its own relevant worker benchmark;
bench-wide propagation does not imply one universal skill or named worker for every recipe.

#### Scenario: Enable and add another recipe
- **WHEN** the player enables Restrict workbench from a bill assigned to a particular pawn and then adds a new bill
- **THEN** every supported bill and the newly added bill use that pawn restriction and the shared skill range.

#### Scenario: Edit through another bill
- **WHEN** another bill on the restricted bench changes the policy to eligible non-mechs with a selected skill range
- **THEN** the contextual scope makes clear that all bills use the new policy, including future additions.

#### Scenario: Named assignment and stored range
- **WHEN** a named worker is outside the stored range but meets the recipe's actual requirements
- **THEN** the stored range does not add a new rejection to that named assignment
- **AND** switching to an applicable non-named choice activates the retained range visibly.

### Requirement: Worker policy composes with complete workbench links
The mod SHALL treat an enabled worker policy as part of the whole linked production plan, with no hidden member-local exception.
Link/join previews include the source plan's policy and any destination restrictions it replaces.
Leaving/dissolving retains the policy with each successor plan under the existing membership rules.
Pasting bills into a restricted plan retains the destination policy and identifies that override.
An incompatible bill family or worker policy prevents the entire affected operation; do not silently
skip one member or grant a different worker to it. Disabling the policy stops propagation but retains
each bill's last effective choices, after which independent lists can be edited separately. Bills
within a still-linked group continue to synchronize each logical row under whole-list linking.

#### Scenario: Two linked benches
- **WHEN** a worker policy is enabled or changed through one linked bench
- **THEN** both benches and all subsequently added logical rows use the same policy.

#### Scenario: Disable without resetting work assignments
- **WHEN** Restrict workbench is disabled on an independent bench
- **THEN** existing bills retain their latest assignments and can thereafter differ
- **AND** new bills use their ordinary individual defaults.

#### Scenario: Paste and incompatible policy
- **WHEN** a copied bill has a different worker choice but the destination plan is restricted
- **THEN** the paste preview identifies the destination override
- **AND** an incompatible destination policy rejects the operation without partial changes.

### Requirement: Restrictions do not supply workers or change physical work
The mod SHALL retain ordinary skill, work-type, faction, slavery, mechanitor, reachability and recipe-specific eligibility alongside the selected policy.
A restriction never makes an otherwise unsuitable pawn eligible. Changes apply to new work; started
work keeps its actual worker, materials and progress subject to native continuation/cancellation rules.
A temporarily absent named pawn causes waiting, not reassignment. An unresolved or permanently lost
named pawn remains visibly unresolved and blocks new affected work until the player chooses a replacement
or deliberately clears the restriction. No fallback silently broadens it to Any worker.

#### Scenario: Restricted pawn lacks the required skill
- **WHEN** a bench is assigned to a pawn unable to perform one of its recipes
- **THEN** that recipe waits under normal eligibility rules rather than receiving a substitute worker.

#### Scenario: Named worker leaves or disappears
- **WHEN** the assigned pawn leaves temporarily or its saved identity can no longer be resolved
- **THEN** the plan waits for the temporary absence or shows explicit recovery for the unresolved identity
- **AND** it does not silently become unrestricted.

#### Scenario: Change during existing work
- **WHEN** a policy changes while another pawn has already begun a supported production iteration
- **THEN** the current physical work is handled by its ordinary continuation rules and future starts use the new policy.

### Requirement: Policy survives lifecycle and fails safely
The mod SHALL retain worker policy through save/load, same-bench minification and reinstallation, with exact ownership and no leakage across saves or similarly named benches.
Destruction removes the destroyed bench's local ownership while a surviving linked plan keeps its
policy. A move to another map requires explicit recovery of map-local choices before new starts.
If policy support is rejected, keep saved policy visible and suspend affected owned work before
the restriction can be lost. Warn once per failed support group and require explicit Resume after
support and references are restored. Unrelated ordinary unrestricted bills remain usable.

#### Scenario: Reinstall and reload
- **WHEN** the same restricted bench is minified, reinstalled on its map and saved/reloaded
- **THEN** its policy and bills retain the same assignments without acquiring another bench's choices.

#### Scenario: Failed support or map transfer
- **WHEN** required policy support is unavailable at startup or a map move leaves its worker choice unresolved
- **THEN** affected new work is stopped with an explanation while the saved policy remains recoverable
- **AND** restoring valid support requires deliberate Resume instead of silent broadening.
