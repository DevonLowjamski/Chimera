**Style Guide v1.2**

Drawing upon the core vision, technical specifications, design philosophies, and creative insights outlined across various development documents, this updated Style Guide provides a comprehensive blueprint for Project Chimera's visual presentation, user interface, and asset creation, integrating details regarding realism data, phased development, risk mitigation, automation, resource management, data collection/analysis, genetics, construction, and progression.

**1\. Overall Vision & Mood:**

* **Core Aesthetic:** The foundation is a **Modern, High-Tech, Clinical/Scientific, Aspirational/Professional**aesthetic. This provides a clean, sophisticated base, particularly for larger-scale operations. Player choice allows for variations, including a **Relaxed & Cozy** feel especially in smaller-scale or early-game setups like the Residential House.  
* **Atmosphere Keywords:** The visual atmosphere should be **Realistic** (emphasizing function and detail over grunge), **Clean**, **Detailed**, **Rewarding**, and **Engaging**.  
* **Visual Priorities:**  
  * **Cleanliness:** Assets and environments must appear **Pristine and well-maintained**, intentionally **avoiding excessive grime, rust, or dirt**, unless specifically tied to a player customization option. This supports the high-tech/clinical aesthetic.  
  * **Detail Focus:** Paramount importance is placed on **intricate equipment models** (lights, pumps, HVAC, lab gear), **granular player customization**, and **clear, detailed UI data visualizations**. Plant visual complexity is initially a lower priority compared to core functional models but will evolve significantly with AI-assisted procedural generation.  
  * **Rewarding Feedback:** The game must provide **clear, frequent visual acknowledgment of player actions, progress, and achievements**. Feedback should be scalable, reflecting both small task completions and major milestones. Intuitive visual upgrades for equipment or facilities reinforce progression.  
  * **Engaging Presentation:** Complex simulation data (environment, genetics, financials, resources) is presented in a clear, accessible, and intuitive manner to empower player decision-making and optimization. This is a **CRITICAL gameplay "asset"**.  
  * **Player Expression:** Systems allow tailoring the facility mood, from a highly-optimized "lab" to a smaller "hobbyist" setup, achieved through equipment scale, layout choices, placement of cosmetic items, and material selections. Unlockable decorative items can also deliver lore.

**2\. Artistic Style Benchmarks & Inspirations:**

* **Overall Goal:** A **slightly stylized but grounded realistic look** is targeted, featuring high-quality, clean graphics and functional detail.  
* **Equipment & Machinery:**  
  * **Primary Reference:** **Satisfactory** (detailed, functional, clean machines/infrastructure).  
  * **Supporting Reference:** **Farming Simulator** series (grounded, functional, realistic equipment).  
  * Avoid: Overly futuristic (Star Citizen), heavily abstracted (Factorio).  
* **UI Data Visualization:**  
  * **Primary References:** **Stellaris, Cities: Skylines** (clear, intuitive complex data/resource presentation, prioritizing graphical representations like icons, graphs, progress bars over text).  
  * Avoid: Overly cluttered/text-heavy interfaces (EVE Online).  
* **Facility Layout & Views:**  
  * **Reference:** **Two Point Hospital/Campus** (adapted for a more serious tone), employing a **blueprint/architectural visualization** for top-level facility management.  
  * The design uses a **hierarchical zoom/view system**: Strategic Facility (Blueprint/Utility View \- Lvl 2\) \-\> Tactical Room (Interior "Street View" \- Lvl 3\) \-\> Detail Views (Bench, Hydro System, Plant \- Lvl 4+) for increasing visual and data granularity.  
* **"Cozy" Small-Scale Vibe:** Inspired by real-world indoor grows ("iGrow," tent/closet setups), featuring less sophisticated equipment, personal decorations, warmer lighting, while maintaining the fundamental graphic quality and realism.  
* **Real-World Design Inspiration:**  
  * **Cannabis Industry:** Demeter Designs (high-end grow rooms), Athena Nutrients (clean, modern branding), Jungle Boys (brand aesthetic), CannaCribs tours (diverse, professional facilities/equipment), Hanna Instruments (scientific/lab equipment).  
  * **General Design:** Apple, Google (clean lines, quality materials, intuitive design, professionalism).  
* **Rendering & Lighting:**  
  * **Rendering:** High-quality, slightly stylized but grounded; detailed, well-rendered, solid, functional assets.  
  * **Lighting:** Primarily **soft & ambient** (supports clean, cozy look). Options for **harsher, functional, or colored lighting** exist for high-tech facilities or specific grow lights.  
* **Physics:** Core simulation models abstract real-world physics for performance. For example, HVAC/Airflow uses a zone-based influence model, not full CFD, and Irrigation/Fluid Dynamics are simplified, focusing on logical flow and resource distribution rather than detailed pipe physics. Visual feedback for utility flow is simple and non-simulated. The focus is on creating a fun and engaging simulation that is understandable, even if not perfectly physically accurate.

**3\. Color Palette:**

* **Overall Aesthetic:** Elegant, sophisticated, modern, clean; beautiful contrast; easy readability. Uses a darker UI motif ("dark mode" feel) with deep, mature colors.  
* **Primary Palette (High-Tech/Default):**  
  * **Dominant:** Expansive Greys (metallic), Mature/Majestic Greens (healthy plants, branding), clean Whites, Light Blues (highlights, contrasting dark UI).  
  * **Usage:** UI foundation, equipment textures, architectural elements; clean backdrop for data/interaction.  
* **Secondary Palette (High-Tech/Default):**  
  * **Dominant:** Charcoal, Off-Black, Deep/Dark Blues, Deep Purples.  
  * **Usage:** Contrast, UI panel borders, text backgrounds, secondary info, darker equipment/materials.  
* **Accent Palette (High-Tech/Default):**  
  * **Dominant:** **Bright, energetic colors inspired by cannabis** (Yellows, Oranges, Reds, Pinks, vibrant Purples).  
  * **Usage:** **Sparingly for highlights, calls to action, selected states, important graph data points, status indicators**. Strong contrast against dark themes. Also used for distinct utility highlighting in X-Ray mode (e.g., blue for water, yellow for electrical).  
* **"Cozy" Palette (Player Choice/Small Scale):**  
  * **Dominant:** Warmer, softer, earthy tones: Beiges, Creams, Light Browns, Wood Tones, Light Greens, Light Blues.  
  * **Usage:** Player customization (materials, decor), smaller setups.  
* **Functional Color Use:**  
  * **Systematic:** Consistent color use for related UI functions/data types (e.g., specific blue for water flow).  
* **Alerts/Rewards (Decision Made):** A **Hybrid Approach** is recommended.  
  * General UI, Positive, Neutral elements use the **existing accent palette**.  
    * **Green (Accent): "Optimal," completion, "System Online," "Healthy"**.  
    * Teal/Purple (Accent): Standard interactive elements, info displays, selected states.  
    * Gold/Orange (Accent): **"Reward" notifications, important non-critical info**.  
  * Dedicated Alert Colors use a **Distinct Palette**.  
    * **Bright Red: Exclusively for CRITICAL, immediate-attention alerts** (e.g., crop loss, fire). Used sparingly for maximum impact.  
    * **Bright Yellow/Amber: WARNINGS** needing timely attention, but not catastrophic (low resources, early pest detected). Palette Orange can be used if distinct enough, otherwise a dedicated yellow/amber.  
  * This approach maintains accent palette integrity while giving critical alerts immediate, understood prominence. Alerts also have optional distinct audio cues.

**4\. Typography:**

* **Overall Goal:** A **clean, modern, highly readable typographic system** that complements the sophisticated UI and detailed data. Features precise, professional fonts aligning with high-tech/scientific themes.  
* **Primary Characteristics:**  
  * **Style:** Prioritizes **sans-serif fonts** for screen clarity.  
  * **Readability:** Highly legible at various sizes for dense data, body text, and UI elements.  
  * **Hierarchy:** Uses a font family (or complementary families) with a **wide weight range** (Light, Regular, Medium, Semi-Bold, Bold, Black). Subtle color variations from the palette can differentiate headers.  
* **Recommended Font Family Approach:**  
  * **Superfamily:** One versatile geometric sans-serif family is preferred. (Initial Preference: Poppins; subject to review/refinement).  
* **Usage Guidelines:**  
  * Headers/Titles: Heavier weights (Bold, Black) for clear section definition.  
  * Body Text/Descriptions: Standard weights (Regular, Medium) for optimal readability.  
  * UI Elements (Buttons, Labels): Medium or Semi-Bold weights for clarity.  
  * **Data Displays (Graphs, Tables, Stats): Clarity is paramount**. Regular or Medium weights are used. **Avoid overly stylized fonts**.  
* Implementation: Ensure fonts are licensed for game use and integrate well with the chosen engine's UI system.

**5\. Rendering Style & Quality:**

* **Overall Goal:** A visually sophisticated, high-quality rendering, aligning with modern graphical standards. **Detail and clean visuals are emphasized, not necessarily photorealism**. Style is **"slightly stylized but grounded"**.  
* **Level of Detail (LOD) & Textures:** Assets (equipment, environments, initial plants) feature **relatively high geometric detail and texture resolution**. Fine equipment details are visible. Prioritize visual fidelity for core launch assets. **LODs are REQUIRED for almost all 3D assets**, especially plants and frequently used equipment, and should be **aggressive and well-optimized**, typically using 3-4 levels per asset.  
* **Material Definition:** Focus on **high contrast, realistic material interactions** (light reflection/absorption). **Perceived realism** in material relationships is important. Metals should be clean, plastics well-defined. PBR (Physically Based Rendering) is used, with AI potentially assisting with texture generation and base meshes. Plant shaders should be good, though not necessarily cutting-edge SSS initially.  
* **Lighting:** Primarily **soft & ambient**, supporting the clean, potential cozy vibe. Contextual, harsher, functional, or colored lighting is used for specific grow lights or high-tech facilities.  
* **Post-Processing / VFX:** Elegant, appropriate use (Ambient Occlusion, Bloom, subtle Depth of Field) matching the overall graphical sophistication; not overused. Particle systems can be used for effects like drips or CO2 haze.  
* **Anti-Aliasing:** **Good quality AA** is needed for smooth edges, supporting the "clean" aesthetic.  
* **Performance Strategy:** For the initial launch, prioritize high visual quality/detail within a constrained scope (e.g., limited max facility size/asset count). Ongoing optimization and performance scaling for larger player creations will occur post-launch. Poly budgets will be TBD during development via performance testing, balancing detail with smooth performance, with complex equipment having higher budgets.

**6\. Environment Design:**

* **Core Concept:** Progression from smaller, predefined spaces (Residential House) to large-scale, customizable sandbox facilities (Warehouse, potential future maps like Greenhouse, Research Lab, Outdoor Field).  
* **Sandbox Environments:** Maps are pre-made, identical for all players to provide a consistent start. Facilities are located in an abstract **"endless white abyss" or "purgatory"**, which limits external environmental storytelling and keeps the focus on the player-controlled interiors.  
* **Interior Focus:** Environmental storytelling is primarily achieved within the player-controlled spaces, through initial state wear/tear, unlockable decorative items with lore, and the initial state/unique challenges of future facility types (e.g., old logs in an Abandoned Research Outpost).  
* **Structural Elements:** Players place walls, floors, roofs in 3D space within map boundaries. This is primarily **grid-based (1ft unit)**, with snapping options. Height/thickness is determined by selecting specific assets (e.g., "8ft Drywall Section" vs. "10ft Insulated Panel"). **Material properties** (cost, appearance, insulation, light/air barrier, cleanliness, durability) are inherent to the chosen construction asset. No separate "paint" tool to change fundamental material in MVP.  
* **Equipment & Furniture Placement:** Essential cultivation equipment and functional furniture (workbenches, shelving, storage) are placed. Placement can be free or grid-snapped, with collision detection and multi-axis rotation. Workflow tools like "Copy and Paste" assist layout. **Placeable storage assets are crucial for maintaining a clean aesthetic** by visually storing unused items.  
* **Zoning:** Players can designate areas (Veg, Flower, Dry, Moms, Clones, Cure). Strategic placement impacts efficiency, environmental control, and risk mitigation. Zoning UI helps manage this.  
* **Utility System Construction:** A **detailed, interconnected network of Plumbing, Electrical, and HVAC systems**is a key feature, unlocked concurrently with the **Warehouse map**. Players manually route pipes, ducts, and wires in 3D. Sizing (diameters/gauges) impacts performance (flow, capacity, pressure/voltage drop). Material impacts cost, durability, efficiency. Logical connections provide visual feedback.  
* **Utility View Toggle ("X-Ray"/"Engineering View"):** **CRUCIAL for complex environments like the Warehouse**. **Not available in the initial Residential House map**. Unlocked with Warehouse access or progression. When toggled, structural elements become **"ghostly translucent"** to reveal hidden networks. Utilities are highlighted using **distinct color-coding** and potentially emissive glow/bold outlines for selected or active components.  
* **Abstracted Microclimate Modeling:** The system simulates localized environmental variations within controlled spaces. Equipment projects a "radius of effect" or "cone of influence", diminishing with distance or obstruction. Key influencing factors include heat (lights, pumps), airflow (fans, HVAC), plant density/transpiration, obstructions, and room construction thermal properties. Visual analysis overlays like **environmental heat maps**can help identify inconsistencies and dead spots. Simple, **non-simulated flow animations** (pulses, color changes) provide visual feedback for utility function. Performance is managed via periodic recalculation, not per frame.  
* **Clutter Management & Facility Aesthetics:** Maintaining a clean, organized visual appearance is a key design goal. Strategies include providing adequate space, encouraging vertical space use, designing assets with clear footprints and using a robust grid/snapping system for orderly placement, and providing placeable storage solutions. Waste management is initially manual transport to disposal assets.  
* **Buffers, Redundancy, Risk Management:** Systems like Backup Power (Generators, Battery Banks/UPS) and redundant water pumps (via plumbing design or researchable equipment) are visual/functional elements crucial for managing risk at scale, especially with time acceleration.

**7\. Plant Visualization:**

* **Core Goal:** Achieve the **highest possible visual realism and biological accuracy for cannabis plants**. They are the **primary visual output of the Genetics x Environment (GxE) simulation**. Plants should strive for maximum fidelity without jarring disconnect from other "stylized but grounded" assets.  
* **Level of Detail:** **Extremely detailed models** are required, showing distinct leaf shapes/serrations, stem/branch structures, realistic bud formation/density, **visible trichomes**, potential root systems (depending on medium viz). Health and stress visual indicators are also crucial.  
* **Dynamic Visual Response (GxE Visualization \- CRITICAL):** Plants must **dynamically change over time and subtly differ individually based on simulation data**. This includes reflecting:  
  * **Genetics:** Inherited traits (structure, leaf shape, bud density/shape, color potential, trichome production).  
  * **Environment:** Real-time conditions (light, temp, humidity, VPD, CO2, water stress) affecting growth, stretch, leaf posture, stress coloration.  
  * **Nutrients:** Visual cues for deficiencies/excesses (yellowing, spotting, curl).  
  * **Health:** Indicators for pests, diseases (mold, mildew), physical damage.  
  * **Growth Stage:** Clear visual progression (seedling \-\> vegetative \-\> flowering \-\> ripening).  
* **Procedural Variation & Consistency:** **Procedural generation**, likely **AI-assisted**, will be driven by simulation data to create **subtle, biologically plausible variations**. This provides nuanced differences and light randomness while ensuring visual consistency within genetic lines (same strain, similar conditions \= highly similar look). Base meshes and textures may be created/assisted by AI tools like Rodin, with a custom AI-Assisted Procedural Generation system handling the dynamic GxE reflection.  
* **Color & Appearance:** Realistic color representation based on genetics and environment is needed, researching strain characteristics for color expressions (greens, purples, reds, oranges, yellows). **Trichome density and appearance** ("frostiness") are a **key visual quality indicator**, reflecting genetics and environment, showing ripeness (clear, milky, amber).  
* **Progressive Implementation:** Full dynamic, hyper-realistic visualization is a long-term goal. Initial launch will feature high-quality base models and core visual responses, with variation/responsiveness evolving iteratively post-launch as simulation complexity increases. Player observation is key early on.

**8\. UI/UX Aesthetics:**

* **Overall Goal:** The UI/UX is a **CRITICAL gameplay asset** aiming to be **modern, clean, sophisticated, visually stunning, intuitive, and highly functional**. It must effectively present complex simulation data without overwhelming the player. It aligns with a **"dark mode" palette**.  
* **Core Philosophy:** Transform intricate simulation data into **intuitive, actionable insights**, empowering players to observe patterns, learn from outcomes, and optimize operations.  
* **Panel & Button Style:** Inspired by **Material Design** for panels and interactive elements. Features subtle depth, layering, and soft shadows for hierarchy and interactivity, with a clean overall look.  
* **Iconography:**  
  * **Style:** Abstract but recognizable symbols, sophisticated line art. Simple, clear, and instantly understandable.  
  * **Color:** Vibrant Accent Color Palette is used for icons to stand out against the dark UI, potentially categorizing functions.  
  * **Contrast:** A flat, line-art style icon contrasts with the Material Design panels to differentiate non-interactive symbols from clickable elements.  
* **Data Visualization:**  
  * **Priority:** **CRITICAL feature**. Displays (graphs, charts, readouts) must be exceptionally clear, easy to interpret, aesthetically pleasing, and genuinely useful for player decisions.  
  * **Techniques:** Uses clean lines, subtle gradients, and the Accent/Functional Color palettes **logically and consistently** to highlight trends, thresholds, optimal ranges, and key data. **Avoids overly complex or cosmetic visualizations that hinder understanding**. Examples include Environmental Data Dashboards (real-time sensor readouts, target vs actual values, status indicators), Graphs & Charts (historical trends, multi-variable plots), Plant Health/Status Indicators, Utility Usage Dashboard, Operational/Financial Data, and Simulated Lab Results. Data can be presented via dedicated UI panels, overlays, or contextual corner pop-ups.  
* **Organization & Navigation:** Uses a **hierarchical view** system (Facility \-\> Room \-\> Plant/Asset). Information complexity is managed via flexible UI organization strategies such as **tabs, collapsible sections, contextual tooltips, and modal windows**. This allows for **progressive disclosure** of information. An "X-Ray" or Utility View is used for complex utility networks.  
* **Interaction & Feedback:** Subtle, smooth UI animations and transitions enhance the user experience without being jarring or disruptive. Visual and optional audio alerts notify players of critical issues. Tool interactions follow a flow of identifying need, selecting the tool, and interacting with the target asset. Data logging interfaces (Laptop, Tablet, Desktop) are visual assets.  
* **Refinement:** UI/UX is subject to **iterative design and usability testing** throughout development. Full UI modding is unlikely for MVP, but limited customization (widgets, presets) may be possible. ADA's communication is primarily text-based UI, with limited synthesized voice-overs for impact.

**9\. Technical Specifications:**

* **Purpose:** Ensure visual assets meet technical standards for quality, performance, and engine compatibility (Unreal Engine is the chosen engine). Essential for both manual and AI-assisted assets.  
* **General:** Strict, documented naming conventions, standardized folder structures, and version control (Git LFS for large binaries) are mandatory. Assets must be importable and usable in the engine with proper material setup.  
* **Textures:** Use a PBR workflow (Metallic/Roughness preferred). Textures should have appropriate resolution based on size and visibility, optimized via atlasing. AI tools like Rodin can assist with PBR textures.  
* **3D Models:** Quality goal aligns with "high geometric detail & texture resolution". Topology should be clean and efficient, avoiding common errors. UV mapping must be clean and non-overlapping for PBR. Polygon budgets will be determined during development based on performance testing, balancing detail with smooth performance in large facilities. Complex equipment will have higher budgets.  
* **LODs:** **MANDATORY for all but the simplest 3D assets**. Multiple, progressively lower-poly versions are required for performance.  
* **CRITICAL WORKFLOW STEP \- MANDATORY MANUAL OPTIMIZATION:** **ALL AI-generated 3D assets MUST undergo manual review and optimization by a human artist**. AI outputs are **NOT game-ready raw**. Key human artist tasks include:  
  * Retopology: Creating a clean, game-ready low-poly mesh.  
  * UV Unwrapping & Correction: Creating professional, non-overlapping UV layouts.  
  * LOD Creation: Manually creating multiple LODs.  
  * Texture Baking & Refinement: Baking high-poly details to optimized textures and refining PBR maps to ensure art style consistency.  
* **AI Tool Integration:** AI is used as **assistive technology** in a **hybrid human-AI workflow**. Humans direct, refine, and optimize. AI tools are used strategically and selectively (ideation, base generation, sub-tasks), **NOT as artist replacement**. A rigorous provenance tracking system is mandatory for AI assets, recording tool, prompts, seeds, date, human modifications, and licensing. This ensures legal/ethical compliance, aids QC, reproducibility, and debugging.  
* **Engine-Native Systems:** Leverage engine capabilities for shaders/materials (dynamic GxE visualization, utility flow), particle systems (drips, haze), and UI animation. This provides better performance, stability, and access to robust tools.  
* **Data-Informed Development:** Analytics tools will be used post-launch to collect anonymized data on player progression, economic behavior, and performance, informing balancing and future development. Continuous monitoring of emerging technology (especially AI) is also strategic, assessing maturity, workflow impact, cost, legality, performance, and alignment with the game vision before adoption.

**10\. Do's and Don'ts:**

* **Do:** **Maintain a clean, pristine aesthetic** for assets and environments.  
* **Do:** **Focus visual detail on equipment models and UI data visualizations**.  
* **Do:** **Use the defined color palettes (Dark Mode Primary/Secondary, Vibrant Accents, Earthy Cozy, Distinct Alerts) consistently and purposefully**.  
* **Do:** **Ensure high readability and clarity** in typography and UI elements.  
* **Do:** **Strive for the highest possible biological realism and dynamic visual response for plants** based on GxE simulation.  
* **Do:** **Employ subtle, non-intrusive UI animations and transitions** to enhance UX without hindrance.  
* **Do:** **Mandate manual technical optimization (retopology, UVs, LODs, texture refinement) for ALL AI-generated 3D assets**.  
* **Do:** **Utilize hierarchical views and clear layout techniques (tabs, collapsible sections, tooltips) to manage information complexity**.  
* **Do:** Present complex simulation data via clear, aesthetic, and useful graphical representations and data visualizations.  
* **Do:** Provide clear visual feedback for valid/invalid placements and utility connections during construction.  
* **Do:** Use visual assets to represent UI/Data Viz systems (Laptops, Tablets, Desktops).  
* **Don't:** Allow excessive grunge, rust, dirt, or clutter in environments or on assets.  
* **Don't:** Use overly stylized fonts or visualizations that hinder readability or data interpretation.  
* **Don't:** Rely solely on raw AI output for game assets; human oversight and optimization are essential.  
* **Don't:** Implement automatic utility routing in MVP; manual routing is required.  
* **Don't:** Have players acquire landrace genetics via physical exploration in MVP; use abstracted narrative methods.