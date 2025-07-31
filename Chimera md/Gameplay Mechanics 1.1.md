# **Project Chimera: Gameplay Mechanics v1.1**

## **1\. Introduction**

This document details the core gameplay mechanics available to the player in Project Chimera. It builds upon the foundational concepts outlined in the Game Concept Document v1.0 and incorporates the specific actions listed in the user-provided "Game Mechanics.docx" file. The focus here is on *what* the player can do and the systems they interact with.  
*(Note: As per user request, detailed mechanics for the Breeding/Genetics Simulation and the underlying Physics Simulation will be elaborated upon in separate, dedicated documents.)*

## **2\. Core Gameplay Loop Summary**

The central experience revolves around a continuous cycle of:

1. **Design & Build:** Constructing and optimizing cultivation spaces and infrastructure within the available map/sandbox.  
2. **Cultivate:** Planting seeds/clones, managing the grow environment, nurturing plants through their lifecycle, and addressing challenges.  
3. **Harvest & Process:** Harvesting mature plants and executing drying/curing protocols to achieve desired end-product quality.  
4. **Analyze & Learn:** Collecting and interpreting data from the grow cycle (environmental, plant health, yield, quality metrics) to inform future decisions.  
5. **Optimize & Expand:** Using knowledge, resources, and potentially new genetics (from breeding \- detailed elsewhere) to improve setups, techniques, and scale operations.  
6. **Progress:** Unlocking new equipment, facility types, genetic potential, and advanced techniques through a dedicated progression system (details TBD).

## **3\. Cultivation Systems & Plant Interaction**

This encompasses all direct interactions with plants and the management of their immediate growing environment.

### **3.1. Plant Lifecycle Management**

* **Planting:**  
  * Starting from Seeds: Sourcing and planting seeds in appropriate starter mediums.  
  * Starting from Clones: Taking cuttings from designated "Mother" plants, preparing them (e.g., rooting hormone), and placing them in propagation environments.  
* **Transplanting / Up-Potting:** Moving plants between containers or growing systems as they mature (e.g., clones to small pots, small pots to final containers/beds). Requires selecting appropriate growing medium volume and type for the next stage.  
* **Plant Training & Shaping:**  
  * **Pruning/Defoliation:** Removing leaves or branches to improve light penetration, airflow, or manage plant structure (Includes Skirting, Lollipopping).  
  * **Topping/Tipping:** Removing the apical meristem to encourage bushier growth and multiple main colas.  
  * **Low-Stress Training (LST):** Bending and tying down branches to create an even canopy and maximize light exposure. Utilizes tools like tie wraps, support posts/stakes.  
  * **Trellising:** Using netting or structures to support plants, spread canopies, and manage growth, especially during flowering.  
* **Plant Movement:** Physically moving plants between designated zones or areas within the facility based on their growth stage or purpose (e.g., Propagation \-\> Veg \-\> Flower \-\> Dry Room).

### **3.2. Irrigation & Fertigation Management**

* **Watering Strategy:** Players define and execute precise watering plans:  
  * **Volume Control:** Setting total water volume per cycle, volume per watering event, and number of events per cycle.  
  * **Frequency Control:** Defining the timing and duration between watering events.  
  * **Advanced Techniques (Crop Steering):** Implementing strategies like controlled drybacks (allowing medium to dry significantly between waterings), managing leachate (runoff), performing flushes (feeding only water), or stacking EC (intentionally raising nutrient concentration at specific times). The system simulates the combined effects of these choices.  
* **Watering Methods:** Implementing and managing different delivery systems:  
  * Manual Hand Watering.  
  * Automated Drip Emitters.  
  * Ebb & Flow / Flood Tables.  
  * Hydroponic Systems (DWC, NFT, etc. \- specific implementation details TBD).  
  * Aeroponic Systems (Misting \- specific implementation details TBD).  
* **Fertigation (Nutrient Delivery):**  
  * **Nutrient Selection:** Choosing fertilizer types (liquid, granular/powder, water-soluble, top-feed amendments, organic vs. synthetic inputs). Considers NPK values, macro/micronutrient profiles, and potential additives.  
  * **Mixing:** Preparing nutrient solutions in tanks, potentially involving multiple parts or additives. Requires careful measurement and order of operations.  
  * **Dosing & Application:** Delivering the nutrient solution via the chosen irrigation method. Includes options for foliar spraying. Requires monitoring and adjusting concentration (EC/PPM) and pH.

### **3.3. Environmental Control**

* **Temperature Management:** Monitoring and controlling temperature in different zones:  
  * Ambient Air Temperature.  
  * Growing Medium Temperature.  
  * Leaf Surface Temperature (relevant for VPD).  
  * Managing Day/Night Temperature differentials.  
* **Humidity Management:** Monitoring and controlling relative humidity (RH):  
  * Ambient Air RH.  
  * Microclimate RH within the canopy.  
  * Managing Day/Night RH differentials.  
* **Lighting Management:**  
  * **Intensity & Delivery:** Adjusting brightness/dimming, fixture height, and layout to achieve target PPFD levels across the canopy. Considers electrical capacity.  
  * **Spectrum & Quality:** Choosing light types (LED, HPS, HID, Fluorescent) with different spectral outputs (PAR). Simulates impact of spectrum on growth stages.  
  * **Photoperiod Scheduling:** Setting light cycles (e.g., 18/6 for Veg, 12/12 for Flower, 24/0, custom schedules). Managing light leaks/bleed during dark periods.  
* **Atmospheric Management:**  
  * **Airflow & Ventilation:** Ensuring adequate air circulation using fans (intake, exhaust, oscillating) to prevent stagnant air, manage temperature/humidity, and distribute CO2.  
  * **CO2 Enrichment:** Optionally supplementing CO2 levels to boost photosynthesis (requires careful monitoring and integration with other environmental factors).  
  * **Vapor Pressure Deficit (VPD):** Monitoring and managing VPD (calculated from Temp/RH) as a key indicator of plant transpiration potential.  
* **Spatial Management:**  
  * **Plant Spacing:** Determining optimal spacing between plants and benches/rows to manage airflow, light penetration, and prevent overcrowding.  
  * **Microclimate Awareness:** Recognizing and mitigating microclimates caused by proximity to walls, vents, or equipment blockage.

### **3.4. Integrated Pest Management (IPM)**

* **Scouting & Identification:** Regularly inspecting plants and the environment:  
  * Visual Inspection of plants for pests, damage, or disease symptoms.  
  * Microscopic Inspection: Taking samples (leaf, stem, medium) for closer examination to identify specific pests, diseases, or potential deficiencies/viruses.  
* **Treatment & Prevention:**  
  * **Mixing & Application:** Preparing and applying pesticides or beneficial treatments (spraying, soil drenching).  
  * **Beneficial Introduction:** Applying beneficial insects or microbes.  
  * **Trapping:** Using tools like sticky traps.  
  * *(Note: Specific pest/disease types and their simulation TBD)*

### **3.5. Harvesting & Post-Harvest Processing**

* **Harvesting:**  
  * Determining optimal harvest time (based on visual cues like trichome appearance \- simulated).  
  * Removing support structures (trellises, stakes).  
  * Cutting down plants (whole plant or branches).  
  * Performing final defoliation if desired (pre-dry trim).  
  * Transporting harvested material to the designated Drying Room.  
* **Drying:** Hanging plants or placing branches/buds on racks in a controlled drying environment (managing Temp, RH, Airflow, Darkness). Monitoring moisture loss.  
* **Curing:** Transferring dried buds to curing containers. Managing RH within containers (e.g., "burping" or using humidity control packs) over time to develop final aroma, flavor, and smoothness.

### **3.6. Cleaning & Sanitation**

* **Routine Cleaning:** Performing regular cleaning tasks:  
  * Dry Cleaning: Sweeping, vacuuming, using leaf blowers (appropriately).  
  * Wet Cleaning: Washing down surfaces (floors, walls, tables, lights, equipment) in grow areas, drying rooms, etc.  
* **System Flushing & Cleaning:**  
  * Irrigation Lines: Flushing lines, cleaning filters, emitters.  
  * Tanks & Mixers: Cleaning nutrient reservoirs and mixing equipment.  
  * HVAC Systems: Cleaning filters, drain pans, potentially ducting.  
* **Deep Cleaning & Sanitization:** Performing thorough cleaning and applying sanitizers between cycles or after contamination events to prevent carryover of pests/diseases. Includes all surfaces and equipment. Utilizes different methods based on scale (e.g., wipe-downs vs. pressure washing).

## **4\. Facility Construction & Management**

This covers the player's ability to design, build, and modify their cultivation facilities using the provided tools and assets.

### **4.1. Structural Elements (Walls, Floors, Roofs)**

* **Placement:** Building structural elements in any direction (X, Y, Z axes) within the map boundaries.  
* **Orientation:** Primarily locked (Walls vertical, Floors/Roofs horizontal), potential for angled elements in future updates?  
* **Sizing:** Defining length, width, height/thickness of elements.  
* **Material Selection:** Choosing materials impacts:  
  * Structural Properties: Strength, durability.  
  * Cost.  
  * Appearance/Cosmetics.  
  * Functional Properties: Insulation (thermal), light interaction (reflectivity, absorption, transparency), airflow interaction.

### **4.2. Equipment & Furniture Placement (Benches, Tables, Racks)**

* **Placement:** Placing items freely or snapped to grid within the buildable area. Considers collision with other objects.  
* **Orientation:** Allows rotation/spinning on multiple axes.  
* **Sizing:** Predefined sizes for most equipment, potentially adjustable for benches/racks.  
* **Material:** Impacts durability, cost, appearance, potentially cleanliness factor.  
* **Functionality:** Provides surfaces for plants, equipment mounting points, etc.  
* **Workflow Tools:** Features like "Copy and Paste" for efficient duplication of layouts.

### **4.3. Utility System Construction**

* **General Properties (All Utilities):**  
  * **Placement:** Routing pipes, ducts, wires in 3D space, snapping to grid or allowing free routing. Considers collisions.  
  * **Orientation:** Full rotation/angling capability for connections.  
  * **Sizing:** Selecting appropriate diameters/gauges impacts performance (flow rate, capacity, pressure drop, voltage drop).  
  * **Material Selection:** Impacts cost, durability, efficiency (e.g., pipe friction, duct insulation, wire resistance), appearance.  
  * **Connectivity:** Systems require logical connections from source (water main, power panel) to endpoints (emitters, lights, equipment). Visual feedback confirms valid connections.  
* **Plumbing & Irrigation:**  
  * Components: Pipes, fittings, valves (manual/solenoid), pumps, filters, tanks (batch, mix), emitters.  
  * Functionality: Simulates flow rate, pressure based on layout, pump power, pipe specs. Requires connection to water source and delivery points.  
* **HVAC & Airflow:**  
  * Components: Ducting, vents (input/output), inline fans, AC units, heaters, humidifiers, dehumidifiers, auxiliary fans.  
  * Functionality: Simulates airflow rate (CFM), temperature/humidity modification capacity based on equipment specs and layout. Requires power connection, potentially water connections (for humidifiers/dehumidifiers).  
* **Electrical System:**  
  * Components: Power sources (Grid connection, Generators, Solar Panels \+ Inverters/Batteries), Panel Boxes (with breakers), Wiring, Outlets, Equipment connections.  
  * Functionality: Simulates power generation/draw, circuit load, voltage. Requires logical routing from source \-\> panel \-\> circuits \-\> outlets/equipment. Breakers trip if circuits are overloaded. Different sources have cost/environmental implications. Equipment requires sufficient power supply.

### **4.4. Zoning & Layout Optimization**

* Players can designate areas within their facility for specific purposes (Veg, Flower, Dry, etc.).  
* Strategic placement of zones and efficient routing of utilities and workflow impacts operational efficiency, environmental control effectiveness, and potentially risk mitigation (e.g., separating clean/dirty areas).

## **5\. Data Collection & Analysis**

Players utilize tools and interfaces to gather information and make informed decisions.

### **5.1. Data Points & Collection Methods**

* **Environmental Data:** Real-time readings from sensors (placed by player or integrated into zones) for Temperature, Humidity, CO2, PAR/PPFD.  
* **Growing Medium Data:**  
  * Manual Sampling: Using handheld meters to measure EC/PPM, pH, Temperature, Volumetric Water Content (VWC%) of the medium itself or leachate/runoff.  
* **Plant Data:**  
  * Visual Inspection: Observing plant health, growth stage, structure, color.  
  * Manual Sampling: Using tools like Chlorophyll meters on leaves.  
  * *(Advanced Analysis \- Future?)* Potential for simulated lab analysis of tissue samples for precise nutrient levels or cannabinoid/terpene profiles post-harvest.  
* **Operational Data:** Tracking resource consumption (water, power), costs, time spent on tasks, yield results.

### **5.2. Data Presentation & UI**

* **Dashboards & Overlays:** Customizable UI elements displaying key real-time data.  
* **Graphs & Charts:** Visualizing trends over time for environmental parameters, nutrient levels, plant growth metrics.  
* **Logs & Notes:** System for recording observations, task completion, harvest results associated with specific plants or batches.  
* **Alerts & Notifications:** System flags critical issues (e.g., environment out of range, nutrient lockout detected, pest outbreak).

## **6\. Economy & Resource Management (Initial Scope)**

* **Financials:** Managing a budget. Costs associated with building materials, equipment purchase, utilities (water, power), nutrients, seeds/clones. Income generated by selling harvested product (to NPC buyers/contracts in initial version).  
* **Resource Inventory:** Tracking quantities of consumable resources (nutrients, growing medium, water supply, fuel for generators, etc.).

*(Note: The full player-driven marketplace and associated economic complexities are planned for a future update.)*