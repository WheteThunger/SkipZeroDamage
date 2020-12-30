## Features

- Prevents processing damage if it would amount to 0.
  - This reduces the number of network updates the server needs to send when entities are taking hits of 0 damage. Networking updates amount to a significant percentage of server performance strain, so this can reduce lag in situations such as fire spam since a typical incendiary rocket can cause 4000-6000 damage events over the course of a few minutes across a few building blocks.
- Optionally prevents processing damage that is really low, such as `0.1` (configurable). Defaults to `0` meaning this feature is disabled.
  - This fixes an issue where an entity cannot be repaired for 30 seconds after taking bullet damage even if the total damage dealt was really low such as 0.005.

## Commands

- `skipzerodamage.report` -- Displays a report of how many damage events were blocked since the plugin was loaded.

Example output:

```
Zero-damage events blocked: 28202
Low-damage events blocked (below 0.01): 42
Low-damage cumulatively blocked: 0.2307883
```

The low damage values only display if the configuration option `LowDamageThreshold` is greater than 0.

## Permissions

- `skipzerodamage.report` -- Allows players to use the `skipzerodamage.report` command of the same name. Without this permission, the command can only be used by an Admin or from the server console.

## Configuration

Default configuration:

```json
{
  "LowDamageThreshold": 0.0
}
```

Any damage below `LowDamageThreshold` will be skipped.
