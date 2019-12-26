## Dogfight - [Video](https://youtu.be/KzP-8lUQdmU)

This is a reinforcement learning space battle demo made with [Unity Machine Learning Agents](https://github.com/Unity-Technologies/ml-agents) [v0.12](https://github.com/Unity-Technologies/ml-agents/releases/tag/0.12.0). The included model was trained with PPO in three stages:

### Obstacle avoidance

After trying various detection methods (omni-directional raycasts, visual observations and [depth camera](https://github.com/mbaske/unity-depth-camera)), I ended up using only seven forward facing sphere casts with a wide radius. Agents control acceleration, pitch and roll. They receive rewards proportional to their forward speed and penalties for proximity to asteroids.

### Follow enemy ships

Each agent can observe the relative positions, orientations and velocities of two opponents at a time - one in front and one behind it. Ideally, it should be up to the agent to decide which opponent it follows, but that would require keeping track of a variable number of targets. To simplify things, enemy ships are sorted by distance and usually the closest ones are being observed. Rewards are assigned like above, but now multplied with the vector dot product of the ships forward axis and the relative direction towards the target.

### Shoot and evade

Agents control fire action and get rewarded for hitting opponents. They receive penalties for being hit themselves, as well as for friendly fire and wasting ammo.