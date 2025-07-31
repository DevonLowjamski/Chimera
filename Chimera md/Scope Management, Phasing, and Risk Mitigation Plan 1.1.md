## **Scope Management, Phasing, and Risk Mitigation Plan v1.1**

**I. Introduction**  
This document outlines Project Chimera's comprehensive strategy for managing project scope, implementing a phased development approach, and proactively mitigating potential risks. The overarching goal is to ensure the successful delivery of a high-quality, deep, and engaging core experience for the Minimum Viable Product (MVP) at initial launch. This plan also establishes a clear roadmap for future expansions and feature additions, allowing for sustainable development and iterative improvement based on technical feasibility, resource allocation, and player feedback. Managing complexity while retaining depth is a central tenet of this plan.  
**II. Feature Prioritization & MVP Definition**  
Given Project Chimera's ambitious multi-genre design, rigorous feature prioritization is paramount for a focused and achievable MVP.  
**A. Core Pillars for MVP (Must-Have Features for Initial Launch):**  
The MVP will focus on delivering a complete and satisfying core gameplay loop, encompassing:

1. **Cultivation Simulation:**  
   * Detailed plant lifecycle management: planting seeds/clones, transplanting, basic plant training techniques (e.g., topping, LST \- manual application).  
   * Environmental Control: Manual adjustment and basic automation (e.g., timers for lights, simple thermostat/humidistat for basic HVAC elements) for temperature, humidity, light cycles.  
   * Nutrient Management: Manual mixing of basic nutrient recipes, application, and monitoring of medium EC/pH (with handheld meters).  
   * Plant Health: Basic visual indicators of plant health, introduction to a few common pests/diseases with manual treatment options (e.g., neem oil spray).  
2. **Genetics & Basic Breeding:**  
   * Simplified inheritance model for a core set of key traits (e.g., primary cannabinoid potential \[THC/CBD\], basic yield factor, flowering time, rudimentary morphology).  
   * Sexual reproduction: Identifying male/female plants, manual pollination to create F1 generation seeds.  
   * Basic Cloning: Taking cuttings from mother plants for genetic preservation.  
   * Phenotype Observation: Players observe and select plants based on visual characteristics and basic post-harvest metrics.  
3. **Facility Construction & Management (Initial Scale \- "Residential House" Map):**  
   * Grid-based construction of interior rooms (walls, doors).  
   * Placement of essential cultivation equipment: grow lights (basic tier), fans, pots/containers, basic irrigation components (e.g., watering cans, simple reservoirs \+ pumps for manual/timed watering).  
   * Basic utility connections (abstracted power draw).  
4. **Post-Harvest Processing (Basic):**  
   * Drying: Manual hanging/rack drying in a designated (potentially player-built) dry space, with environmental factors impacting drying time and quality.  
   * Curing: Manual curing in containers (e.g., jars), including manual "burping" process.  
   * Trimming: Manual hand-trimming of harvested buds.  
5. **Economy (NPC-Driven):**  
   * NPC-issued cultivation contracts with specific strain/quantity/quality requirements.  
   * Direct sales of harvested product to a limited set of NPC buyers.  
   * Management of basic operational costs: utilities (electricity), consumables (nutrients, basic growing medium, seeds), initial equipment purchases.  
6. **Player Progression (Core Loop):**  
   * Initial branches of the Skill Tree focusing on core cultivation, basic genetics, and facility operation.  
   * Unlocking new equipment tiers and basic techniques through gameplay achievements and skill point allocation.  
   * Narrative guidance via AI Advisor (ADA) and initial objectives leading to the Warehouse map unlock as a major progression milestone.  
7. **Data, UI, & Feedback Systems (Essential):**  
   * Core environmental data dashboards for real-time sensor readouts.  
   * Plant status UI panels with essential health and growth information.  
   * Basic logs & notes system for player observations and key automated event logging.  
   * Essential alert system for critical environmental deviations or plant health issues.

**B. Features Prioritized for Simplification or Deferral (Post-MVP or Later Phases):**  
To maintain a manageable scope for the MVP, the following features will be simplified or deferred:

* **Advanced Breeding Systems:** Complex polygenic trait modeling, detailed genetic marker analysis, advanced tissue culture techniques, and genetic modification (CRISPR-like mechanics) will be *simplified for MVP (focus on observable traits and simple inheritance) and significantly expanded post-launch*.  
* **Player-Driven Marketplace:** Deferred entirely to a post-MVP expansion, as per "Game Concept 1.3." The MVP will rely solely on the NPC economy.  
* **Advanced Automation & Robotics:** Complex late-game automation systems (e.g., robotic potting, fully automated harvesting lines, advanced conveyor systems) will be *deferred to late-game progression within later MVP development stages or fully to post-MVP expansions*. Basic automation (timers, simple controllers) will be in MVP.  
* **Advanced Extraction & Product Formulation:** Production of oils, concentrates, isolates, edibles, and topicals is *deferred to post-MVP expansions*. The Asset List confirms "Extraction/Concentrate Equipment... post-MVP."  
* **Complex Physics Models:** Highly detailed fluid dynamics in pipes or computational airflow simulations will use abstracted, performant models for MVP (see Section VI.A).  
* **Multi-Region Gameplay & Advanced Exterior World Simulation:** The MVP will focus on the core facility maps (Residential House, basic Warehouse access) set within the "white abyss." Dynamic geographical regions, distinct climates, and detailed exterior environments are post-MVP, as per "Construction & Facility Management Design 1.1."  
* **Deep Narrative Threads & Complex NPC Interactions:** The MVP will feature the functional AI Advisor (ADA) and basic NPC contract givers. Deeper character development, intricate storylines, and complex NPC relationship systems can be explored post-MVP.  
* **AI Research Lab (for breeding prediction):** This advanced predictive tool for breeding is *deferred to late-game content or a post-MVP genetics-focused expansion*.

**III. Refining Scope & Phased Development Plan**  
A phased approach will ensure iterative development, risk management, and the ability to incorporate learnings.

* **Phase 0: Pre-Production & Prototyping (Completed & Ongoing where necessary)**  
  * Core design documentation, visual style guides, technical planning.  
  * Prototyping high-risk systems: abstracted physics for environment, basic procedural plant generation concepts, core UI interactions.  
* **Phase 1: Vertical Slice & Core Loop Validation**  
  * **Objective:** Implement and rigorously test the absolute core gameplay loop: planting a seed, basic environmental management (manual/simple auto), plant growth cycle, harvest, basic drying/curing, and a rudimentary NPC sale interaction. Establish core performance benchmarks.  
  * **Key Systems:** Basic plant growth model (visual stages, response to light/water/basic nutrients), simplified environmental factors, placeholder UI for critical data.  
  * **Outcome:** A playable, demonstrable vertical slice that validates the fun factor and technical feasibility of the core simulation.  
* **Phase 2: MVP Development (Iterative Sprints)**  
  * **Objective:** Build out the full feature set defined for the MVP, focusing on iterative development and integration.  
  * **Sprint Structure (Illustrative):**  
    * *Module 1: Cultivation & Environment:* Detailed plant lifecycle, nutrient system implementation, first pass on pest/disease system, basic HVAC and sensor integration, environmental dashboard V1.  
    * *Module 2: Genetics & Basic Breeding:* Simple trait inheritance, F1 cross functionality, cloning mechanics, phenotype observation tools, basic Genetics Lab UI.  
    * *Module 3: Facility & Economy Core:* Residential House map finalization, grid-based construction refinement, core equipment set implementation, NPC contract system V1, resource/utility cost tracking.  
    * *Module 4: Post-Harvest & UI/UX Pass 1:* Drying/curing mechanics implementation, manual trimming system, core data visualization (graphs, logs), ADA implementation (basic tutorial/guidance), alerts system V1.  
    * *Module 5 onwards: Content Expansion, Polish & Balancing:* Adding more strains, equipment tiers, refining UI/UX based on internal testing, balancing progression (Skill Tree, research costs), initial tutorial content, optimization passes.  
  * **Methodology:** Agile sprints with regular internal playtesting, review sessions, and continuous integration.  
* **Phase 3: Alpha & Beta Testing**  
  * **Alpha:** Feature-complete MVP. Focus on internal QA, bug fixing, performance optimization, and initial balancing.  
  * **Closed Beta:** Controlled external testing with a limited player group to gather feedback on gameplay, usability, balance, and technical stability.  
  * **Open Beta (Optional):** Broader stress testing and final feedback gathering before launch.  
* **Phase 4: MVP Launch & Initial Post-Launch Support**  
  * Release of the polished MVP.  
  * Focus on critical bug fixes, server stability (if any online features exist day 1, though player market is deferred), and immediate player feedback.  
* **Phase 5: Post-MVP Expansions & Live Service (Long-Term)**  
  * Development of deferred features and new content based on MVP success, player feedback, and strategic roadmap.  
  * Examples: Player-Driven Marketplace Expansion, Advanced Genetics & Research Expansion, Industrial Operations & Automation Expansion, New Maps/Narrative Content.

**IV. Re-evaluating the AR Component**  
The original game concept included an Augmented Reality (AR) "search/find" component for acquiring rare genetics like landraces. This has been critically re-evaluated.

* **Assessment:**  
  * **Cost/Benefit:** AR development is technically complex, resource-intensive, platform-dependent (iOS/Android nuances), and introduces significant testing overhead. The direct benefit to a core simulation-heavy game like Project Chimera is questionable compared to the substantial development cost and risk. It can often feel like a tacked-on gimmick if not deeply and meaningfully integrated.  
  * **Technical & Design Challenges:** GPS accuracy, variable real-world environments, user privacy concerns, maintaining engagement with an AR feature alongside the deep PC simulation.  
  * **Game Concept 1.3 Decision:** The "Game Concept 1.3" document explicitly notes "AR removal" to allow for a more focused initial development on the core cultivation and genetics loops.  
* **Alternative In-Game Mechanics for Rare Genetic Acquisition:**  
  * **NPC-Sponsored Expeditions/Research Grants:** Players invest resources/time into (abstracted) expeditions to remote in-game regions, with a chance of returning rare landrace seeds. Success could depend on research unlocks or specific equipment.  
  * **High-Tier NPC Contacts & Faction Reputation:** Gaining high reputation with specific NPC entities (e.g., remote agricultural collectives, university botanical departments, legacy seed banks) unlocks access to their exclusive heirloom or landrace genetics.  
  * **Lore-Driven Discovery & "Lost Strain" Quests:** Players might find clues in old in-game scientific journals, historical records, or through NPC dialogue, leading to a multi-stage objective to "rediscover" a forgotten or rare landrace.  
  * **Specialist NPC Vendors or Collectors:** Rare genetics may occasionally appear for sale from unique, high-priced NPC vendors or be offered as rewards for completing exceptionally challenging contracts.  
* **Final Decision: Defer AR Component Indefinitely.** The team will focus on developing robust, immersive, and thematically consistent in-game mechanics for the acquisition of landraces and other rare genetic material. This aligns with the strategic decision in "Game Concept 1.3" and significantly reduces MVP scope risk.

**V. Managing Complexity Balancing**  
A core challenge is ensuring Project Chimera's systems are deep and realistic yet remain engaging, understandable, and strategically meaningful, rather than becoming impenetrably complex or tedious. The "Realism Bias" must be balanced with playability.

* **Strategies for Balanced Complexity:**  
  * **Progressive Disclosure of Mechanics:** Players are introduced to systems gradually. Simpler versions of mechanics are presented early, with layers of complexity and advanced options unlocked via Skill Tree progression, research, and facility upgrades.  
  * **Clear & Intuitive UI/UX:** As detailed in the "Data, UI, and Feedback Systems" document, effective data visualization, contextual tooltips, clear alerts, and an organized UI are paramount for player comprehension.  
  * **AI Advisor (ADA) Guidance:** ADA will provide contextual explanations for new mechanics, highlight important information, and offer tips without being overly intrusive.  
  * **Integrated Tutorial System & Guides:** An in-game tutorial will cover fundamentals. A "Plant Problems Guide" (from Asset List) and accessible help menus will explain core concepts, common issues, and potential solutions.  
  * **"Earned Automation" Philosophy:** Manual tasks will be designed to be feasible for small-scale operations, demonstrating the "Burden of Consistency" as operations scale or time accelerates. This makes automation a desirable and rewarding progression, as per "Balancing Manual Tasks vs. Automation 1.1," rather than a punishment for not automating.  
  * **Focus on Meaningful Strategic Choices:** Complexity should result in interesting decisions with clear cause-and-effect relationships that players can learn from, not just an overwhelming number of variables to micromanage.  
  * **Continuous Playtesting & Iteration:** Regular playtesting with internal teams and, later, external testers (beta phases) is crucial to identify areas of excessive complexity, confusion, or tedium. Systems will be refined based on this feedback.  
  * **Strategic Abstraction:** Where hyper-realism adds overwhelming complexity for minimal gameplay benefit (e.g., simulating the precise chemical reactions in nutrient solutions or quantum physics of light), mechanics will be abstracted to a believable and manageable level.

**VI. Addressing Technical Risks Proactively**  
Identifying and mitigating technical risks early is key to a smooth development cycle.

* **A. Complex Physics Model Performance (HVAC/Airflow, Irrigation):**  
  * **Risk:** Full simulation of fluid dynamics or CFD for airflow is computationally prohibitive for a game.  
  * **Mitigation Strategy:**  
    * Implement **Abstracted Physics Models** as detailed in "Technical & Systemic Implementation": zone-based environmental influences, periodic (not per-frame) updates, simplified flow logic.  
    * **Early Prototyping & Profiling:** Develop and stress-test these abstracted systems early to establish performance baselines and identify bottlenecks.  
    * **Scalability Testing:** Ensure models perform adequately as facility size and equipment density increase. Optimize algorithms and data structures continuously.  
* **B. Player-Driven Economy Stability (Post-MVP Risk):**  
  * **Risk:** Inflation, exploits, item duplication, and market manipulation can destabilize a player-driven economy.  
  * **Mitigation Strategy (for future implementation, planned from outset):**  
    * Design and implement multiple **Robust Resource/Currency Sinks** from the start of marketplace development (e.g., transaction taxes, listing fees, high-value NPC services, repair costs) as detailed in "Economy & Marketplace Systems."  
    * Carefully balance resource generation rates ("faucets") to prevent oversupply.  
    * Incorporate **Market Monitoring & Analytics Tools** to track economic health and detect anomalies.  
    * Conduct extensive **Closed Beta Testing** of the marketplace before full release to identify and patch exploits.  
* **C. Procedural Plant Generation Quality & Performance:**  
  * **Risk:** Generating visually diverse, high-quality plants that accurately reflect genetics and environment, while maintaining performance with many instances, is challenging.  
  * **Mitigation Strategy:**  
    * Adopt a **Modular and Artist-Controlled Procedural System** ("Technical & Systemic Implementation"). Human artists create high-quality base assets, textures, and define generation rules. AI may assist in creating variety within these rules.  
    * Enforce **Strict Performance Budgets** (poly counts, draw calls, texture memory) and aggressive LODs for all procedurally generated plants.  
    * Conduct **Extensive Visual & Performance Testing** across a wide range of genetic combinations and environmental conditions.  
* **D. AI-Assisted Asset Pipeline Integrity & Legal Compliance:**  
  * **Risk:** Over-reliance on immature AI tools, inconsistent art style, legal/ethical issues regarding copyright of AI-generated content, inefficient integration into PBR workflows.  
  * **Mitigation Strategy:**  
    * Enforce **Mandatory Human Artist Optimization and Oversight** for ALL assets that have any AI-assisted component, as detailed in "Technical & Systemic Implementation."  
    * Implement **Rigorous Provenance Tracking** for all AI-assisted assets (tool used, prompts, human modifications).  
    * Use AI tools **Strategically and Selectively** for ideation, base generation, or specific sub-tasks, not as a replacement for skilled artistry.  
    * Maintain strict adherence to the **Visual Style Guide** through human review checkpoints.  
    * Stay continuously informed on the **Evolving Legal and Ethical Landscape** surrounding AI-generated content and ensure all tool licenses are respected.

**VII. Securing Realism Data & Knowledge Base**  
Grounding the game's simulation in accurate and verifiable information is crucial for authenticity and aligns with the "Realism Bias" principle.

* **Process for Sourcing and Validating Data:**  
  1. **Foundation of Public-Domain Knowledge:** Compile an initial knowledge base from reputable public sources: peer-reviewed scientific papers on plant physiology and cannabis science, university agricultural extension publications, established horticultural textbooks, and respected cultivation guides.  
  2. **Expert Consultation (Considered, Subject to Budget/Availability):** Explore the possibility of consulting with botanists, horticulturalists, cannabis geneticists, or highly experienced (legal) cultivators under NDA to validate core simulation assumptions, gather nuanced insights, and review abstracted models for plausibility.  
  3. **Cross-Referencing and Consensus:** Information will be cross-referenced from multiple credible sources to identify established scientific consensus and differentiate it from anecdotal claims or highly speculative theories.  
  4. **Focus on Established Scientific Principles:** The simulation will prioritize modeling based on well-understood biological and chemical processes.  
  5. **GxE (Genotype x Environment) Interaction Modeling:**  
     * Identify key environmental factors (light spectrum/intensity, temperature, humidity, VPD, CO2, specific nutrient availability/deficiency/toxicity, water stress) and their known, significant impacts on cannabis plant morphology, growth rates, health, and secondary metabolite production (cannabinoids, terpenes).  
     * Develop simplified but believable mathematical or logical models to simulate these GxE responses, focusing on observable outcomes rather than deep molecular simulation.  
  6. **Genetic Trait Data:** Define a core set of heritable traits (e.g., cannabinoid ratios, terpene profiles, yield potential, flowering time, structural characteristics, pest/disease resistance). Establish plausible ranges and simplified inheritance patterns (dominant/recessive for some, abstracted polygenic effects for others) based on available cannabis genetics literature.  
  7. **Iterative Refinement & In-Game Documentation:** The "realism data" implemented will be subject to ongoing review and refinement throughout development and post-launch based on internal testing, player feedback, and new scientific information. Key validated information will be presented to players through in-game guides like the "Plant Problems Guide" and ADA's contextual explanations.

**VIII. Conclusion**  
This Scope Management, Phasing, and Risk Mitigation Plan provides a strategic framework for the development of Project Chimera. By focusing on a well-defined MVP with core features, deferring higher-risk and non-essential elements to later phases, and proactively addressing technical and complexity challenges, the project aims for a sustainable and successful development cycle. A commitment to iterative development, rigorous testing, securing accurate realism data, and strategically integrating AI tools under human oversight will be crucial in realizing the ambitious vision of a deep, engaging, and authentic cannabis cultivation and genetics simulation. This structured approach will guide Project Chimera toward a strong initial launch and lay a solid foundation for its future growth and evolution.