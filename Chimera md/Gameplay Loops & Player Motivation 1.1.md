**Core Gameplay Loops & Player Motivation Refinement (DRAFT v1.1)**

**Document Purpose:** To define the foundational minute-to-minute and hour-to-hour gameplay loops, the underlying time mechanics, player progression systems (with a focus on the Skill Tree), and the motivational principles guiding the player experience in Project Chimera.

**Overall Philosophy:** Project Chimera aims to deliver an engaging and rewarding cannabis cultivation simulation. The core experience emphasizes player observation, learning through experimentation, and a strong sense of progression. Early gameplay focuses on manual interaction to build an appreciation for later automation and advanced analytical tools. The design intends to create a cycle where detailed management and optimization lead to tangible improvements in cultivation outcomes and facility capabilities, experienced at a player-controlled pace and driven by intrinsic and extrinsic motivators aligned with Self-Determination Theory (Autonomy, Competence, Relatedness).

**A. Core Gameplay Loop: Minute-to-Minute Interactions & Systems**

1. **Time Mechanic Integration:**

   * All minute-to-minute loops operate within a flexible, player-controlled time system (detailed in "Project Chimera: Time Mechanic \- DRAFT v1.1").  
   * The active in-game time scale directly impacts the rate of visual change in plants, the frequency of required manual interventions, the speed at which environmental conditions can drift or respond to adjustments, and the real-world duration to observe outcomes.  
   * In-game timestamps are crucial for all logged data.  
2. **Navigation & View Modes:**

   * **Hierarchical Zoom Navigation:** Overall facility view (strategic) down to section/room "street view" (tactical).  
   * **Individual Asset Focus:** Clicking a plant or equipment enters a focused "Action Mode" or detailed UI panel.  
3. **Core Minute-to-Minute Interaction Loops:**

   * **Plant Observation & Status Check Loop:**  
     * Scan area \-\> Navigate to plant \-\> Visual inspection (correlated to simulated health) \-\> Click for "Plant Detail UI."  
     * **Plant Detail UI (Evolves with Progression):**  
       * **Initial:** Strain Name, Plant Age (progresses with in-game clock), Overall Health Status Bar (game-determined, visually correlated), Player Visual Observation Log. Blank fields for advanced data (with tooltips hinting at unlock requirements).  
       * **Advanced:** Populates with manual tool readings (with in-game timestamps) and real-time sensor data. Organized with tabs (Environment, Nutrition, Genetics, etc.) for clarity.  
   * **Manual Data Acquisition Loop (Tool-Based):**  
     * Identify need \-\> Select tool from inventory (cursor change, tool visible) \-\> Click target asset (plant, substrate, room for ambient readings) \-\> Enter focused "Action Mode" (tool-specific view, e.g., pH meter in substrate, thermometer displaying local ambient temp).  
     * Observe animated tool readout \-\> Data auto-logged (with in-game timestamp) \-\> Exit Action Mode.  
   * **Manual Plant Work Loop (e.g., Pruning):**  
     * Identify need \-\> Select tool (e.g., shears) \-\> Click target plant \-\> Enter "Action Mode" (e.g., plant structure view with targetable, highlighted parts).  
     * Perform action (click to cut) \-\> Visual/audio feedback, plant model updates \-\> Exit Action Mode.  
   * **Manual Environmental Adjustment Loop (Early Game):**  
     * Observe environmental data \-\> Navigate to basic device (fan, vent) \-\> Click device \-\> Enter "Action Mode" (device control UI).  
     * Adjust setting \-\> Observe device animation/UI feedback \-\> Later observe environmental/plant impact.  
4. **Core Principles for Minute-to-Minute Interactions:**

   * Visual feedback is primary and directly reflects underlying simulation.  
   * Learning through doing, within a player-controlled time framework.  
   * Direct player agency over tools and basic equipment.  
   * Data is a progressively richer resource.  
   * Consistent interaction flows (select tool \-\> select target \-\> action mode).  
   * **No Distracting On-Plant Visual Indicators for Issues (Early Game):** Players must rely on direct observation and learn to interpret plant visuals and basic data, fostering the "earned insight" that makes later diagnostic tools and sensors valuable.

**B. Core Gameplay Loop: Hour-to-Hour Session Structure & Systems**

1. **Time Mechanic Integration:**

   * The real-world duration of in-game processes (plant growth stages, research, contracts) is determined by the player's chosen active time scale.  
   * Resource consumption rates (in real-time) and the urgency of tasks scale with game speed.  
   * Players choose an offline time progression speed (from paused to active rates) at session end, with outcomes reviewed via a "Catch-Up Visualization" and "Facility Status Report" upon login.  
2. **Session Goals & Objectives:**

   * **Explicit, Story-Influenced Objectives:** Provided via a dedicated UI (e.g., "Contracts," "Projects"), narratively contextualized to guide players and teach mechanics.  
   * **Time-Sensitivity:** Objectives may have in-game deadlines, with real-world urgency depending on active time scale.  
   * **Achievable Scope:** Designed for significant progress or completion within a 1-2 hour session.  
3. **Player Progression Systems (Primary Driver: Skill Tree \- "Trees"):**

   * **Skill Point Acquisition:**  
     * Main Source: Completing objectives/tasks/challenges.  
     * Secondary Source: Successful harvests (quality/outcome-based reward).  
   * **Skill Tree ("Trees") Visual Metaphor:** A cannabis plant, with 7 primary "Leaves" (categories) of varying size/prominence reflecting importance. Leaves unfurl to show "Nodes" (skills/concepts). Plant visually grows/becomes more vibrant with progression.  
   * **Node Unlocking Philosophy:** Unlocking a node introduces the *core concept* and its associated game mechanics/simulations. *Mastery and efficiency* over that concept are then achieved via separate equipment/tool progression (purchased, researched, or crafted).  
   * **Skill Tree Categories & Node Counts (Revised Draft v1.1):**  
     * **Genetics (8-12 Nodes):** Seed & Clone Fundamentals, Sexual Reproduction Basics, Phenotype Scouting, Vegetative Propagation Advancement, Pollen Management, Trait Stabilization (Backcrossing), Targeted Reproduction (Feminization/Autoflower), Advanced Propagation (Tissue Culture), Understanding Polygenic Traits, (Late Game) Genetic Marker Assisted Selection.  
     * **Cultivation (6-8 Nodes):** Foundational Plant Care, Growing Media & Containers, Plant Structuring (Pruning/LST), IPM Fundamentals, Optimized Nutrient Delivery (Solution Crafting/Basic Hydro), Advanced Plant Shaping (HST/Canopy Management), Atmospheric Optimization for Cultivation (VPD/CO2).  
     * **Environment (6-8 Nodes):** Core Environmental Parameters (Temp/Humidity/Light Cycle), Air Exchange & Circulation, Climate Control (Heating/Cooling Basics), Atmospheric Refinement (Humidification/Dehumidification), Advanced Lighting Solutions, Environmental Automation (Basic Sensors/Controllers), Precision Climate Management (Integrated HVAC/Advanced Automation).  
     * **Construction (4-6 Nodes):** Basic Infrastructure (Tents/Power Strips), Room Structuring & Layout, Utility Fundamentals (Electrical Circuits/Manual Water Lines), Automated Utilities Installation (Plumbing/Advanced Electrical), Specialized Facility Development (Sealed Rooms/Workflow Optimization).  
     * **Harvest (4-6 Nodes):** Harvest Readiness & Techniques, Controlled Drying Processes, Trimming & Preparation, Curing Science & Application, Post-Harvest Efficiency (Bulk Processing/Basic Automation).  
     * **Science (4-6 Nodes):** Observation & Record Keeping, Manual Environmental & Plant Sampling, Data Interpretation & Diagnostics, Quantitative Analysis (Basic Lab Testing), Advanced Analytics & Research Methodology.  
     * **Business (3-4 Nodes):** Basic Operations Management (Contracts/Simple Finances), Brand & Reputation Building, Market Awareness & Product Specialization, (Optional for Player Market) Advanced Economic Operations.  
   * **Skill Tree Interdependencies:** Logical prerequisites exist between nodes in different categories (as detailed in "Skill Tree Interdependency Map"), encouraging broader development. UI will contextually show these dependencies (highlights, tooltips).  
   * **"Ability vs. Challenge" Dynamic:** Each unlocked skill tree node grants new abilities/access to new mechanics but also introduces new complexities or management considerations, ensuring balanced progression.  
   * **Equipment & Resource-Based Progression:** Distinct from skill tree, players upgrade individual equipment (lights, pumps, etc.) and improve resource quality (water, air) via currency/research, enhancing their ability to manage concepts unlocked in the skill tree.  
   * **Narrative & World-Based Progression:** A light narrative guides players, contextualizes objectives, and can gate progression to larger facilities (e.g., house to warehouse).  
   * **Unlockable Content & Feature Progression:** Major new areas (e.g., warehouse map) or game systems are unlocked via narrative or objective completion.  
   * **Meta-Progression:**  
     * **Persistent Genetic Library:** Player-bred strains tied to player account, usable across saves.  
     * **Starting Facility Choice:** Ability to start new games in advanced facilities (e.g., warehouse) after initial completion.  
     * **Persistent Reputation (Company/User):** Affects NPC interactions (prices, contracts) and player-to-player trading (trust, value). Influenced by product quality and ethical market behavior.  
   * **Skill and Mastery-Based Progression:** Natural player improvement through practice, understanding, experimentation. Potentially reflected in online leaderboards.  
   * **Time-Based and Engagement Progression:** Daily/weekly challenges/rewards, seasonal content/cosmetic drops (tied to real-world time).  
   * **Player Agency and Choice-Driven Progression:** Core sandbox philosophy allowing diverse paths and solutions.  
4. **Rewards for Progression:**

   * In-game currency, Research Points, Blueprints, Reputation.  
   * (Genetics are primarily self-bred, traded, or from special events, not direct quest rewards).  
5. **Core Activities within an Hour-to-Hour Session:**

   * Facility Expansion & Build-Out (physical construction, infrastructure).  
   * System Optimization & Experimentation (refining setups, testing techniques).  
   * Resource Management (inventory, purchasing, planning).  
6. **Session Flow & Sense of Accomplishment:**

   * **Login & Assess:** Review status, objectives, offline progress.  
   * **Execute Routines:** Perform minute-to-minute tasks.  
   * **Work Towards Session Goal(s):** Contracts, research, build projects, experiments.  
   * **Progression & Unlocks:** Claim rewards, make upgrade choices.  
   * **Implement Upgrades & Optimize:** Integrate new capabilities.  
   * **Prepare for Session End:** Ensure facility stability, choose offline time progression mode.  
   * **Satisfaction:** Derived from goal completion, unlocks, visible improvements, facility stability, and anticipation of harvest/future progress.

**C. Balancing Manual Tasks vs. Automation ("Earned Automation")**

1. **Realism First:** Manual tasks mirror real-world effort for small scales. Diligent players can succeed manually.  
2. **"Burden of Consistency" Drives Automation:** The primary challenge is maintaining precision and consistency manually, especially with scale or accelerated time. This burden makes automation desirable.  
3. **Graduated & Realistic Consequences:** Suboptimal conditions (from manual error or poor automation) have realistic, graduated impacts, not instant catastrophic failure for minor issues. Peak genetic potential requires near-perfect conditions.  
4. **Player Agency in Identifying Pain Points:** Players automate tasks they find most challenging or time-consuming.  
5. **Automation Enhances Capability:** Improves consistency, efficiency, scalability, risk mitigation, and frees player time.  
6. **Designing "Initial Tedium Threshold":** "Tedium" arises from the cognitive load and time for consistent manual excellence, not artificial difficulty.  
   * *Manual Watering/Nutrients/Environment/Pest Scouting/Record Keeping:* Natural inconsistencies and time demands of these tasks become more apparent with scale/speed.  
7. **Clear Path to & Benefits of Automation:** Skill Tree and equipment progression clearly present automation solutions. Early automation provides significant, immediate QoL improvements.

**D. Communicating Time & Gameplay Information (UI/UX)**

1. **Persistent Time Display:** Shows current in-game date/time and active time acceleration level/description.  
2. **Contextual Time Info:** Plant growth UI, research/construction queues, contract timers display estimates in both in-game time and real-world time (at current speed).  
3. **Toggleable Time Display Format (Global):** Player can click the main clock/time display to switch all relevant UI elements between "In-Game Time" and "Estimated Real-World Time (at current speed)." A clear visual indicator shows the active mode.  
4. **Historical Logs & Timestamps:** Primarily use in-game date/time for consistency, with optional real-world session metadata.

**E. Application of Player Motivation Theories (Self-Determination Theory \- SDT)**

* **Autonomy:** Supported by player choice in Skill Tree progression, equipment upgrades, facility design, breeding paths, problem-solving approaches, and offline time management.  
* **Competence:** Built through mastering Skill Tree nodes, successfully upgrading and utilizing equipment, achieving high-quality harvests, overcoming challenges introduced by new mechanics, building a strong reputation, and performing well in (optional) leaderboards/challenges. The "Ability vs. Challenge" dynamic is central to this.  
* **Relatedness:** Primarily fostered through the (future) player-driven marketplace (trust, reputation in trades), online leaderboards (shared community goals, comparison), and potentially through interaction with well-developed NPC entities or a sense of contributing to an in-game "scientific community.