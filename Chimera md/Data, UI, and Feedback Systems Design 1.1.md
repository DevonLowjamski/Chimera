## **Data, UI, and Feedback Systems Design v1.1**

**I. Introduction**  
This document outlines the design philosophy and specific implementation details for Project Chimera's Data, User Interface (UI), and Feedback Systems. These systems are fundamental to player understanding, decision-making, and overall engagement with the game's complex cultivation, breeding, and facility management mechanics. The core philosophy is to transform intricate simulation data into intuitive, actionable insights, empowering players to observe patterns, learn from outcomes, and optimize their operations effectively. The UI will strive for a clean, functional, and scalable design, accommodating both novice players and data-driven veterans. This aligns with the "Gameplay Mechanics 1.1" document which notes UI/Data Viz as crucial assets.  
**II. Detailed Design of UI/Data Visualization Elements**  
The game will employ a variety of UI elements to present data in an accessible and meaningful way.  
**A. Environmental Data Dashboards/Overlays**

* **Functionality:** Provide real-time and summarized environmental data for selected grow rooms or specific zones within rooms. This is crucial for at-a-glance status checks and immediate response to changing conditions.  
* **Content:**  
  * Real-time sensor readouts: Temperature, Relative Humidity (RH), Vapor Pressure Deficit (VPD), CO2 levels, and light intensity (PPFD at canopy level if sensors are appropriately placed). Sourced from player-placed sensors as detailed in "Gameplay Mechanics 1.1".  
  * Target vs. Actual Values: Clearly display the player-defined target ranges alongside current readings for easy comparison.  
  * Status Indicators: Color-coding (see Section VIII) or icons to indicate if parameters are within optimal, warning, or critical ranges.  
* **Customization & Accessibility:**  
  * Players can select which specific data points are visible on a main dashboard or a contextual room overlay.  
  * Ability to create and save custom dashboard presets for different tasks (e.g., "Vegetative Stage Overview," "Flower Room \- Late Stage Criticals," "Drying Room Status").  
  * Displayed as a non-intrusive overlay (toggleable visibility) or a dedicated "Facility Overview" screen.  
* **Visual Style:** Clean, highly legible fonts. Digital-style readouts. Minimalist design to avoid distraction.

**B. Graphs & Charts**

* **Functionality:** Enable visualization of historical data trends and relationships between multiple variables, essential for learning and optimization.  
* **Types & Features:**  
  * **Historical Trend Graphs:** Line graphs displaying selected environmental parameters (temp, RH, VPD, CO2), nutrient solution data (EC, pH in reservoirs), plant growth metrics (e.g., height, estimated biomass increase if modeled), and resource consumption over player-defined periods (e.g., last 24 in-game hours, last 7 in-game days, entire grow cycle). ("Gameplay Mechanics 1.1")  
  * **Multi-Variable Plots (Advanced Feature):**  
    * Scatter plots to visualize the correlation between two variables (e.g., VPD vs. a simulated transpiration rate).  
    * Potential for parallel coordinate plots for advanced users to analyze multiple variables simultaneously.  
  * **Comparison Tools:**  
    * Ability to overlay trend graphs from different grow cycles for the same strain/room.  
    * Side-by-side chart comparisons for different strains or different rooms within the same period.  
* **Interface:** Interactive charts allowing players to hover over data points for exact values and timestamps, zoom in/out on timeframes, and toggle data series on/off.

**C. Plant Health/Status Indicators & Dedicated Plant Detail View UI**

* **At-a-Glance Indicators:**  
  * Visual icons or health bars displayed above individual plants (when zoomed in) or on group/bench summaries in the room view. These offer a quick assessment of overall plant well-being (e.g., a leaf icon that changes color from vibrant green to yellow to red, or a segmented health bar). Aligns with "Overall Health Status Bar" from "Core Gameplay Loop 1.1."  
* **Plant Detail View UI:** Accessed by selecting an individual plant. This is a comprehensive panel showing:  
  * **Basic Info:** Strain Name, Unique Plant ID, Plant Age (days/weeks), Current Growth Stage.  
  * **Environmental Snapshot:** Current environmental readings directly affecting that plant (e.g., canopy temperature, local humidity – derived from nearby sensors or microclimate simulation).  
  * **Nutrient Status:** Qualitative assessment (e.g., "Optimal," "Minor Nitrogen Deficiency," "Calcium Toxicity Suspected"). This is derived from a combination of visual plant model cues, player-logged observations, and (if available) simulated leaf/medium analysis data.  
  * **Watering Status:** Current medium moisture level (e.g., VWC%), time since last watering, or indicators like "Needs Water," "Watered."  
  * **Health Issues Log:** A list of currently active or recently resolved problems (e.g., "Pest Detected: Spider Mites \- Low Infestation," "Powdery Mildew \- Treated," "Light Stress \- Minor"). Each entry could link to the "Plant Problems Guide" (mentioned in Asset List as a Tutorial/Info Overlay) for diagnostic and treatment information.  
  * **Genetic Summary:** Key genetic traits (dominant/recessive markers if known), parentage.  
  * **Individual Plant Log/Notes:** Access to logs and notes specifically recorded for this plant.  
  * **Yield Prediction (Flowering Stage):** A very rough, dynamic estimate of potential yield based on current health, size, and genetic potential (if such a predictive model is feasible).

**D. Resource Consumption UI & Bulk Storage Visualization**

* **Resource Overview Panel:**  
  * Clear UI icons for core consumable resources: Water (differentiating sources like Tap, RO, Treated if applicable), Power (grid vs. generator), Nutrients (by type or mixed solutions), CO2 (tank levels). Sourced from "Asset List 1.2."  
  * Displays current stock levels (e.g., liters in water tank, kWh in battery, kg of nutrient powder).  
  * Shows current or recent average consumption rates (e.g., L/hour for water, kWh/day for power).  
* **Bulk Storage Models:**  
  * Where feasible, visual models of large storage containers (water tanks, nutrient totes, CO2 cylinders, fuel tanks for generators) will dynamically reflect their current fill levels. For instance, a translucent water tank model might show the water level rising and falling.  
* **Utility Usage Dashboard:** A dedicated screen or section providing:  
  * Breakdown of power consumption by room, by major equipment category (e.g., Lights, HVAC, Pumps), or even by individual high-draw appliances if players install per-appliance meters (advanced feature).  
  * Similar breakdown for water usage.  
  * Cost calculations for utilities over selected periods.

**III. Specifics of Data Collection & Analysis Tools (Interfaces & Functionality)**  
These tools allow players to actively gather, manage, and interpret data beyond passive displays.  
**A. Sensor & Controller Management Interface**

* **Functionality:** A centralized UI for managing all deployed sensors and environmental controllers.  
* **Features:**  
  * Visual placement mode for sensors within rooms.  
  * Ability to name/label individual sensors (e.g., "Flower Room 1 \- Canopy Sensor Left").  
  * Calibration routine (if calibration drift is a mechanic) – e.g., using a reference tool to adjust sensor accuracy.  
  * Status indicators for each sensor (Online, Offline, Low Battery, Requires Calibration).  
  * Interface for linking specific sensors to environmental controllers and defining their logic (e.g., "IF Sensor X reads Temp \> 28°C, THEN Activate AC Unit Y").

**B. Grow Cycle Comparator Tool**

* **Functionality:** Enables detailed side-by-side analysis of different grow cycles to facilitate learning and optimization.  
* **Interface:**  
  * Players select two or more completed (or ongoing, for partial comparison) grow cycles from their history.  
  * The tool presents a dashboard comparing key performance indicators (KPIs):  
    * Total Yield, Yield per Plant, Yield per Watt/Square Foot.  
    * Quality Metrics (average cannabinoid/terpene profiles from lab tests).  
    * Resource Costs (total power, water, nutrients consumed).  
    * Cycle Duration.  
    * Incidence of health problems.  
  * Allows for overlaying historical trend graphs (e.g., compare the temperature curve of Cycle A vs. Cycle B).  
  * Highlights statistically significant differences, helping players identify factors that led to better or worse outcomes.

**C. Simulated "Lab Analysis" Interface**

* **Functionality:** Provides players access to (simulated) advanced analytical testing for their plants and products.  
* **Interface:**  
  * Players "submit" samples (e.g., a leaf cutting for nutrient analysis, a dried bud sample for cannabinoid/terpene profiling, a soil/medium sample for EC/pH/nutrient composition).  
  * The interface shows a queue of submitted samples, estimated processing time (in-game), and associated costs.  
  * Results are delivered as a detailed "Lab Report" view:  
    * Clear presentation of analysed values (e.g., NPK levels in leaf tissue, percentage of THC/CBD/specific terpenes).  
    * Comparison to optimal ranges or previous samples if applicable.  
    * These results then populate the relevant data fields for the plant, batch, or medium.  
  * This aligns with "Gameplay Mechanics 1.1" ("Advanced Analysis \- Future?").

**IV. Logs & Notes System**  
A robust system for players to record their own observations and for the game to automatically log key events.

* **Player-Driven Logging:**  
  * **Functionality:** A digital "Grower's Journal" allowing players to create free-text notes, observations, and reminders.  
  * **Features:**  
    * Assign notes to specific entities: individual plants, batches of plants, grow rooms, equipment, or general facility operations.  
    * Tagging system: Allow players to apply keywords (e.g., "deficiency," "pest\_sighting," "training\_technique," "F2\_pheno\_hunt") to notes for easy searching and filtering.  
    * Rich text formatting options (bold, italics, lists) for clarity.  
    * Ability to (conceptually) attach screenshots from the game (represented by a thumbnail or link in the note).  
* **Automated Event Logging:**  
  * The game automatically logs critical events with in-game timestamps (as per "Time Mechanic 1.1"):  
    * Planting, transplanting, and harvesting dates for each plant/batch.  
    * Activation of environmental alerts (e.g., "Temperature exceeded critical high in Flower Room 1").  
    * Changes to nutrient solutions (date, recipe name/EC/pH).  
    * Significant player actions (e.g., "Applied IPM Treatment X to Veg Room 2," "Switched lights in Flower Room 3 to 12/12 cycle").  
    * Completion of research, construction, or breeding projects.  
* **Interface:** A central, searchable, and filterable logbook. Players can filter by date range, plant ID, room, event type, or player-defined tags.

**V. Refining Alerts & Notifications**  
Ensuring players are promptly and clearly informed of critical issues.

* **Categories & Triggers (Beyond Basic Environmental Parameters):**  
  * **Environmental Criticals:** Sustained deviation from setpoints that threaten plant health (e.g., extreme temperature, humidity, CO2, or VPD impacting plant stress limits).  
  * **Equipment Malfunctions:** Pump failures, light outages, fan failures, HVAC system faults, controller offline.  
  * **Resource Depletion Warnings:** Critically low levels of water in main reservoirs, nutrient stock solutions, CO2 tank pressure, or fuel for backup generators.  
  * **Plant Health Emergencies:** Detection of severe pest infestations (based on player scouting/tagging or advanced sensor data if implemented), widespread disease symptoms, acute nutrient toxicity/deficiency events impacting multiple plants.  
  * **Operational Alerts:** Contract deadlines approaching, research completion, construction finished, new genetics unlocked.  
  * **Process Flow Issues:** E.g., drying room full when harvest is ready, curing containers needing attention (burping).  
* **Presentation & Prioritization:**  
  * **Visual:**  
    * Non-Modal Pop-ups: For warnings and informational alerts, possibly at a corner of the screen.  
    * Modal Alerts (Central): For critical, game-impactful events that require immediate player acknowledgement.  
    * Flashing Icons/Indicators: On the affected room, equipment in the facility view, or on relevant dashboard elements.  
    * Persistent Alert List/Ticker: A scrollable list of active and recent alerts, color-coded by severity.  
  * **Audio:** Optional, distinct audio cues for different severities (e.g., a gentle chime for informational, a persistent beep for warnings, an urgent alarm for criticals). Players should be able to customize audio alert preferences.  
  * **Actionability:** Alerts must clearly state:  
    * The nature of the problem.  
    * The specific location/entity affected.  
    * The severity/urgency.  
    * Ideally, provide direct links to relevant UI panels or tools (e.g., "Flower Room 2: Pump P-05 Failure. \[View Utility Controls\] \[Go to Location\]").

**VI. Applying Data Visualization Best Practices**  
Presenting complex data in a way that is intuitive, actionable, and aesthetically pleasing.

* **Clarity & Simplicity:** Prioritize readability. Use clean lines, ample negative space, and avoid unnecessary visual clutter ("chart junk"). Data-ink ratio should be high.  
* **Consistency:** Maintain consistent use of colors, icons, typography, and interaction patterns across all data displays and UI elements. This reduces cognitive load.  
* **Visual Hierarchy:** Ensure that the most important information is presented most prominently (e.g., using size, color, placement). Critical alerts should demand attention.  
* **Contextual Information & Guidance:**  
  * **Tooltips:** Extensive use of on-hover tooltips to explain data labels, icons, technical terms, optimal ranges, or how a particular metric is calculated.  
  * **Integrated Help/Tutorials:** Link complex charts or data views to relevant sections in the in-game tutorial or "Plant Problems Guide."  
* **Interactivity:**  
  * Allow players to hover over chart elements for precise data point values.  
  * Enable drill-down capabilities (e.g., click on a summary statistic to see the detailed data behind it).  
  * Provide robust filtering and sorting options for data tables and logs.  
* **Accessibility Considerations:** Offer options for color-blind friendly palettes. Ensure text is scalable or legible at various resolutions.  
* **Avoid Information Overload:** Employ progressive disclosure. Start with summaries and allow players to access more details as needed. Use tabs, collapsible sections, and modal windows to organize dense information effectively.

**VII. Designing Workflow & Facility Layout Optimization Tools (Advanced/Late-Game Features)**  
Tools to help players analyze and improve the physical and operational efficiency of their facilities, especially at larger scales.

* **Visual Analysis Overlays (Toggleable Views):**  
  * **Material Flow Visualization (Abstracted):** An overlay that conceptually shows the paths and potential bottlenecks for key materials:  
    * Water and nutrient solution delivery to grow rooms.  
    * Movement of plants (seedlings to veg, veg to flower, flower to harvest).  
    * Harvested material flow to drying, trimming, curing, and packaging areas.  
    * Waste removal paths.  
  * **Environmental Heat Maps:** As detailed in "Construction & Facility Management Design 1.1." Visual overlays for temperature, humidity, airflow, or light intensity distribution within rooms, helping to identify inconsistencies, dead spots, or areas overly affected by equipment. This relies on the Abstracted Microclimate Modeling.  
  * **Utility Load Visualization:** Overlays on the electrical or plumbing views showing current load on circuits or pipes. Highlights overloaded systems or areas with insufficient capacity.  
* **"Layout Simulation" Mode (Very Advanced Concept):**  
  * A sandboxed planning mode where players can virtually rearrange equipment, add/remove walls, or reconfigure utility lines within an existing facility.  
  * The game would then provide a *simulated projection* of the potential impacts of these changes on key metrics like airflow patterns, temperature distribution, material flow efficiency, or construction costs, *before* the player commits resources to making the actual changes. This would be a powerful, but complex, late-game tool.

**VIII. Decision on Alerts/Rewards Color Use & Palette Application**

* **Context:** The "Visual Style Guide" outlines a core palette (Primary, Secondary, Accent \- Teal, Orange, Purple, Gold, Green).  
* **Recommended Hybrid Approach:**  
  * **General UI & Positive/Neutral Status:**  
    * Utilize the existing accent palette for standard UI elements, highlights, positive feedback, and neutral status indicators.  
    * *Green (Accent):* Primarily for "optimal" conditions, task completion confirmation, positive status (e.g., "System Online," "Healthy").  
    * *Teal/Purple (Accent):* For standard interactive elements, information displays, selected states.  
    * *Gold/Orange (Accent):* Can be used for "Reward" notifications (contract bonuses, research breakthroughs, rare discoveries), and to draw attention to important but non-critical information or items needing moderate attention (e.g., a task ready for collection, equipment nearing maintenance).  
  * **Dedicated Alert Colors (Distinct from general accents for immediate impact):**  
    * **Bright Red (Dedicated Alert Color):** *Exclusively* for CRITICAL, immediate-attention-required alerts. This color should be used sparingly to maintain its high impact (e.g., imminent crop loss, fire, critical equipment failure).  
    * **Bright Yellow/Amber (Dedicated Alert Color):** For WARNINGS that are serious and require timely attention but are not immediately catastrophic (e.g., resources running low, environment drifting significantly but not yet critically, early pest/disease detection requiring investigation). Palette Orange could serve this if it's distinct and impactful enough from Gold. If not, a dedicated yellow/amber is better.  
* **Rationale:** This hybrid approach maintains the integrity and sophistication of the core accent palette for general UI aesthetics and positive reinforcement, while ensuring that critical and warning alerts have immediate, universally understood visual prominence.

**IX. Refining UI Layout & Information Density (Iterative Process)**  
Achieving an optimal balance between providing comprehensive data and maintaining a clean, uncluttered interface requires iterative design and testing.

* **Key UI Organization Strategies:**  
  * **Flexible Tab Systems:** Group related information and functions within main UI panels using tabs (e.g., in a Room Detail view: "Overview," "Environment Controls," "Plants," "Tasks," "Log").  
  * **Collapsible Sections/Accordions:** Present summary information by default, with detailed subsections that can be expanded by the player when needed. This manages information density effectively.  
  * **Contextual Tooltips & Popovers:** Extensive use of on-hover tooltips for icons, data labels, buttons, and unfamiliar terms. More substantial explanatory text or mini-guides can appear in popovers when clicking an info icon.  
  * **Modal Windows:** Use for focused tasks that require dedicated input or present critical information that must be acknowledged (e.g., detailed breeding project setup, end-of-cycle reports, critical alert acknowledgement).  
  * **"Drill-Down" Information Architecture:** Design interfaces to allow players to start with high-level summaries and progressively drill down into more granular details as they require.  
* **Prototyping & Usability Testing:**  
  * Create interactive prototypes (low-fidelity wireframes to high-fidelity mockups).  
  * Conduct usability testing with representative players throughout the development process to identify pain points, assess clarity of information, and gather feedback on layout and flow. This is essential for refining the UI.  
* **Customization (Considerations for Limited Flexibility):**  
  * While full UI modding is likely out of scope, consider allowing players minor customizations, such as choosing which widgets appear on their main dashboard, rearranging the order of some list items, or saving filter presets.

**X. Conclusion**  
The Data, UI, and Feedback Systems are the lens through which players perceive and interact with the entirety of Project Chimera's simulated world. A commitment to clarity, intuitiveness, actionable insights, and iterative design based on best practices and player feedback will ensure these systems effectively support the core gameplay loop of observation, learning, and optimization. These systems will empower players to navigate the game's depth, make informed strategic decisions, and ultimately achieve mastery in their virtual cannabis cultivation and breeding endeavors.