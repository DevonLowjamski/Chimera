**Project Chimera: Time Mechanic \- DRAFT v1.1**

**Core Philosophy:** The time mechanic in Project Chimera aims to provide players with significant control over the pacing of their experience, balancing realism and engagement. It allows for accelerated active gameplay to see biological processes unfold more rapidly, while also offering options for offline progression that respect player agency and the persistence of their cultivation efforts. The system is designed with inherent trade-offs and risks associated with time acceleration, encouraging thoughtful decision-making and rewarding well-managed, automated facilities.

**I. Active Gameplay Time Scales & Controls:**

* **Baseline Time Scale (1x):**

  * **Definition:** 1 in-game week (composed of 6 in-game days) \= 1 real-world hour.  
  * **Equivalency:** 1 in-game day \= 10 real-world minutes.  
  * **Purpose:** This is the default speed, designed to offer a balanced progression where players can manage tasks without feeling overly rushed, and biological processes unfold at a pace that allows for observation and intervention. It's the intended primary speed for engaged gameplay.  
* **Player-Controlled Time Acceleration Levels:**

  * **UI Control:** A UI slider with predefined, clearly labeled snap-to levels. Labels will emphasize the real-world time equivalent for one in-game day to ensure intuitive understanding.  
  * **Proposed Levels (Labeling based on In-Game Day : Real-World Time):**  
    * **"Scientific Observation" (1 In-Game Day \= 24 Real-World Hours):**  
      * *Purpose:* Primarily for players seeking the utmost realism, for detailed, slow-paced observation of plant development, or for specific "hardcore" challenge scenarios. Not intended for standard progression.  
      * *Subtle Benefit:* May offer the highest potential for nuanced genetic expression or quality outcomes.  
    * **"Deliberate Pace" (1 In-Game Day \= 20 Real-World Minutes):**  
      * *Purpose:* For players who prefer a slower, more methodical pace, allowing more real-world time per in-game day.  
    * **"Standard Cultivation" (1 In-Game Day \= 10 Real-World Minutes):** (This is the 1x Baseline)  
      * *Purpose:* The default, balanced experience.  
    * **"Accelerated Growth" (1 In-Game Day \= 4 Real-World Minutes):**  
      * *Purpose:* For experienced players or those with moderately automated setups looking to speed up common grow phases.  
    * **"Rapid Cycle" (1 In-Game Day \= 2 Real-World Minutes):**  
      * *Purpose:* For significantly faster progression, demanding well-automated systems and quick decision-making.  
    * **"Hyper-Cycle" (1 In-Game Day \= 1 Real-World Minute):**  
      * *Purpose:* The fastest active play speed, suitable for very advanced, highly stable, and robustly automated facilities. Carries the highest risk if issues arise.  
      * *Subtle Detriment:* May have a slight cap or reduction in the maximum potential for genetic expression or quality outcomes compared to slower speeds.

**II. Consequences & Management of Active Time Acceleration:**

* **Proportional Task Frequency:** All daily or regularly scheduled in-game tasks will need to be performed proportionally more often in real-world time as game speed increases.

* **Consistent Resource Consumption (Per In-Game Unit of Time):** Resource consumption per *in-game day* remains constant. At accelerated speeds, these resources deplete faster in *real-world time*.

* **"Transition Inertia" System for Time Scale Changes:**

  * **Concept:** To prevent exploitation and encourage deliberate use, changing time speeds will involve a "Transition Inertia" period during which the player cannot initiate another speed change.  
  * **Initiating Change:**  
    1. **Warning & Confirmation:** A clear pop-up will explain the risks/benefits and the Transition Inertia mechanic. Player must confirm.  
    2. **Transition Inertia (Ramp & Lock):** The actual change in game speed will ramp up or down over a calculated "transition duration." During this ramping period, the player cannot initiate further time scale changes.  
       * *Duration Calculation:* This duration could be a percentage (e.g., 5-10%) of the real-world time it takes for one in-game day to pass at the *slower* of the two speeds being transitioned between. This makes large jumps in speed more gradual and locks the player into the transition.  
  * **Purpose:** Makes changing speeds a strategic, committed decision, reflecting system inertia.  
* **Subtle Time Scale-Dependent Variables (Risk/Reward):**

  * **Genetic Expression Potential:** Slower speeds may offer slightly higher *maximum potential* for quality traits (e.g., 1-3% variance in peak THC between slowest/fastest speeds).  
  * **Stress Event Probability/Severity (To be Brainstormed):** Faster speeds might slightly increase base probability/severity of minor stressors if not perfectly managed. Slower speeds might allow more nuanced positive interactions. (Requires careful balancing and extensive testing).

**III. Offline Time Progression:**

* **Player Choice at Session End:** When saving/exiting, players choose how in-game time progresses:  
  * **Paused:** No in-game time passes. (Default for security).  
  * **Selected Active Time Scale:** Any active scale can be chosen for offline progression.  
* **Offline Simulation & Login Resolution:**  
  * Ideally, a detailed simulation runs based on facility state and chosen speed.  
  * **Login Process & "Catch-Up" Visualization:**  
    1. Game calculates/simulates occurred events.  
    2. An initial segment is processed.  
    3. Player sees an **accelerated visual time-lapse** of their facility showing simulated events.  
    4. Game continues simulating remaining offline period in background, seamlessly feeding into the time-lapse.  
    5. **Purpose:** Provides engaging visual feedback and masks computation time.  
  * **Post-Time-Lapse Recap Screen:** A detailed "Facility Status Report" summarizes resources, crop progress, significant events, harvests, and critical alerts.  
* **Risk & Automation:** Offline safety is directly tied to the robustness of automated systems and resource buffers.

**IV. Time and Biological/Game Processes:**

* **Plant Growth Stages:** Base durations (in in-game days) are subject to the active/chosen offline time scale. Player agency can influence some stage lengths (e.g., vegetative). Quality impacts (e.g., from curing duration) are tied to *in-game time*.  
* **Other Game Processes:** Research, construction (if not instant), contract deadlines, etc., adhere to the active in-game time scale.  
* **Global Events ("Season Drops"):** Operate on a fixed real-world calendar, independent of player time scale.

**V. Communicating Time to the Player (UI/UX):**

* **Persistent Time Display:** An always-visible UI element will display the current in-game date and time, and the current time acceleration level (e.g., "Day 42, 14:30 (Rapid Cycle \- 1 IGD \= 2 RWM)").  
* **Contextual Time Information:**  
  * **Plant Growth UI:** Show projected real-world time remaining for current growth stage, dynamically updating based on current time acceleration (e.g., "Flowering: 10 game days remaining / Est. 20 real mins at current speed").  
  * **Research/Construction Queues:** Display estimated real-world completion times based on current speed.  
  * **Contract/Objective Timers:** Clearly show deadlines in both in-game time and estimated real-world time at the current speed.  
* **Toggleable Time Display Format (Game Time vs. Real World Time):**  
  * Players will have the option to click on any time/date string displayed in the UI.  
  * This click will toggle *all* time-related displays throughout the game globally between showing values in "In-Game Time" (e.g., "Finishes in 3 Game Days") and "Estimated Real-World Time at Current Speed" (e.g., "Finishes in approx. 30 Real Minutes").  
  * A clear, persistent visual indicator will show which display mode (Game Time or Real Time) is currently active, ensuring no confusion. This could be a small icon next to the main clock or a subtle change in the font color/style of time displays.  
* **Historical Logs:** Timestamps in logs (e.g., manual data collection, event occurrences) should primarily use in-game date/time for consistency within the game world's chronology, but could offer a real-world timestamp as a secondary piece of metadata if useful.

**VI. Time Scale and Leaderboards/Challenges (Future Consideration):**

* Chosen time scales and efficiency can be integrated into competitive elements.

