**Project Chimera: Core Gameplay Loop Definition (Minute-to-Minute & Hour-to-Hour) \- DRAFT v1.1**  
**Document Purpose:** To consolidate and detail the foundational concepts for Project Chimera's minute-to-minute and hour-to-hour gameplay loops, as discussed and refined. This document incorporates the established Time Mechanic and its influence on player experience.  
**Date:** May 17, 2025  
**Overall Philosophy:** The core gameplay loops are designed to be engaging and rewarding, emphasizing player observation, learning through experimentation, and a strong sense of progression. Early gameplay will involve significant manual interaction, fostering an appreciation for later automation and advanced analytical tools. The loops aim to create a cycle where detailed management and optimization lead to tangible improvements in cultivation outcomes and facility capabilities, all experienced at a player-controlled pace.

### **I. Minute-to-Minute Gameplay Loops & Interactions**

The minute-to-minute experience is centered around direct interaction with plants, the immediate grow environment, and the foundational tools and equipment available to the player, all influenced by the currently selected in-game time scale.  
**A. Navigation & View Modes:** (Largely unchanged by time, but the *rate* of observed change in the environment will scale with game speed).

1. **Hierarchical Zoom Navigation:** ...  
2. **Individual Asset Focus:** ...

**B. Core Minute-to-Minute Interaction Loops:**

1. **Plant Observation & Status Check Loop:**  
   * **Initiation:** ...  
   * **Visual Inspection:** ... The *rate* of visual change (growth, stress manifestation) will be proportional to the active in-game time scale.  
   * **Plant Detail UI Access:** ...  
   * **Information Review (Plant Detail UI):**  
     * **Early Game (No/Basic Tools):**  
       * **Strain Name:** ...  
       * **Plant Age:** Tracks days/weeks in current growth stage, progressing according to the in-game clock.  
       * **Overall Health Status Bar (1-10 Scale):** Game-determined value... The *speed* at which this health status can change (improve or decline) is influenced by the active time scale.  
       * **Visual Observation Log (Player-Input):** ...  
       * **Blank Data Fields:** ...  
     * **Mid-Late Game (With Tools/Sensors):** ... Manual data entries will include an **in-game timestamp**, critical for tracking changes over accelerated or varied time periods.  
     * **Data Organization:** ... Historical data and graphs will clearly show trends against the **in-game timeline**.  
   * **Outcome:** Player gains an updated understanding of the plant's status... The perceived urgency to act will be higher at faster time scales.  
2. **Manual Data Acquisition Loop (Tool-Based):**  
   * **Initiation:** Player identifies a need for specific data... The *frequency* of needing new data points may increase at faster time scales as conditions change more rapidly in real-time.  
   * **Tool Selection:** ...  
   * **Targeting:** ...  
   * **Entering "Action Mode":** ...  
   * **Data Observation & Logging:** Player observes the reading... Data is auto-logged with an **in-game timestamp**.  
   * **Exiting "Action Mode":** ...  
   * **Outcome:** Player acquires a new data point...  
3. **Manual Plant Work Loop (e.g., Pruning, Training, Pest Treatment):**  
   * **Initiation:** Player identifies a need for direct plant intervention... The *rate of plant growth* (influenced by time scale) will dictate the frequency of tasks like pruning or training.  
   * **Tool Selection:** ...  
   * **Targeting & "Action Mode":** ...  
   * **Performing Action:** ...  
   * **Exiting "Action Mode":** ...  
   * **Outcome:** Task completed... The *time until the benefits* of such work become apparent will scale with game speed.  
4. **Manual Environmental Adjustment Loop (Early Game, Basic Equipment):**  
   * **Initiation:** Player observes environmental data... Environmental drift may occur faster at accelerated time scales.  
   * **Equipment Interaction:** ...  
   * **Entering "Action Mode":** ...  
   * **Adjustment:** ...  
   * **Feedback:** ...  
   * **Observation & Learning:** Player subsequently monitors environmental data... The real-world time taken to observe the impact of adjustments will be shorter at faster game speeds.  
   * **Outcome:** Device setting changed...

**C. Core Principles for Minute-to-Minute Interactions:**

* **Visual Feedback is Primary:** ...  
* **Learning Through Doing:** ... Experienced within a player-controlled time framework.  
* **Direct Agency:** ...  
* **Data as a Developing Resource:** ... Acquired and interpreted against the backdrop of the in-game clock.  
* **Consistency in Interaction:** ...

### **II. Hour-to-Hour Gameplay Loop & Session Structure**

The hour-to-hour gameplay loop builds upon the minute-to-minute interactions, focusing on achieving larger objectives, making significant facility upgrades, and seeing tangible progress within a typical 1-2 hour play session, with the duration of in-game processes being relative to the chosen time scale.  
**A. Session Goals & Objectives:**

1. **Explicit, Story-Influenced Objectives:**  
   * ...  
   * **Time-Sensitivity:** Some objectives may have **in-game deadlines** (e.g., "Deliver X by \[In-Game Date\]"). The real-world time available to meet these deadlines will vary based on the player's chosen time acceleration.  
   * **Duration of Tasks:** The real-world time required to complete objectives involving biological processes (e.g., "Grow 5 plants to the vegetative stage") will be directly affected by the active time scale.  
   * **Early to Mid-Game Objective Examples:** ...

**B. Progression & Rewards:**

1. **Reward Types for Objective Completion:** ...  
2. **Player Agency in Progression:** ... Progression through research or construction may involve **in-game time durations**, which translate to variable real-world time based on active speed.

**C. Core Activities within an Hour-to-Hour Session:**

1. **Facility Expansion & Build-Out:**  
   * ... If construction has an in-game time component, its real-world duration will scale.  
   * ...  
2. **System Optimization & Experimentation:**  
   * ... The real-world time needed to run experiments and observe results over several in-game days or weeks will be managed by the player's use of time acceleration.  
   * ...  
3. **Resource Management:**  
   * ... The rate of resource depletion in real-time is directly tied to the active time scale. Managing reserves becomes more critical at faster speeds.  
   * ...

**D. Session Flow & Sense of Accomplishment:**

1. **Typical Session Structure:**  
   * **Login & Assessment:** Review facility status... If offline time progression was active, this includes reviewing the "Catch-Up Visualization" and "Facility Status Report."  
   * **Routine Execution:** ...  
   * **Goal-Oriented Activity:** ...  
   * **Progression & Upgrades:** ...  
   * **Preparation for Session End (Stability Check & Offline Time Choice):** Ensure the facility is stable. Crucially, the player will **choose the desired time scale for offline progression** (from paused to an active speed), accepting the associated risks and potential rewards.  
2. **Sources of Satisfaction & Accomplishment:**  
   * ...  
   * Successfully designing a facility that remains stable and productive during a period of chosen offline time progression.  
   * The ultimate payoff cycle culminating in a successful harvest... The real-world time investment for this payoff is managed by the player through time controls, balancing speed against potential subtle quality benefits of slower, more "realistic" pacing.

### **III. Communicating Time & Gameplay Information (UI/UX)**

To effectively manage gameplay loops influenced by dynamic time, clear communication is essential:

* **Persistent Time Display:** An always-visible UI element will display:  
  * Current in-game date and time.  
  * Current active time acceleration level (e.g., "Day 42, 14:30 | Standard Cultivation \- 1 IGD \= 10 RWM").  
* **Contextual Time Information in UI Elements:**  
  * **Plant Growth UI:** Will show projected real-world time remaining for the current growth stage, dynamically updating based on the current time acceleration (e.g., "Flowering: 10 game days remaining / Est. 1hr 40m real time at current speed").  
  * **Research/Construction Queues:** Will display estimated real-world completion times based on the current active speed.  
  * **Contract/Objective Timers:** Will clearly show deadlines in both in-game time and estimated real-world time at the current speed.  
* **Toggleable Time Display Format (Game Time vs. Real World Time):**  
  * Players will have the option to click on any primary time/date string displayed in the UI (e.g., the main clock).  
  * This click will globally toggle *all* relevant time-related displays throughout the game between:  
    * **"In-Game Time"** (e.g., "Completes in 3 Game Days, 4 Hours").  
    * **"Estimated Real-World Time (at current speed)"** (e.g., "Completes in approx. 30 Real Minutes").  
  * A clear, persistent visual indicator (e.g., an icon like "IGT" or "RWT" next to the main clock, or a subtle style change) will show which display mode is currently active, ensuring no confusion.  
* **Historical Logs & Timestamps:**  
  * Timestamps in logs (e.g., manual data collection, event occurrences, environmental alerts) will primarily use **in-game date/time** for consistent chronology within the game world.  
  * They may offer a secondary real-world timestamp (of when the event occurred in the player's session) as metadata if useful for player reference.

This integration should make the relationship between the core gameplay loops and the Time Mechanic much clearer. The player's ability to control and understand time is central to how they will experience and strategize within Project Chimera.