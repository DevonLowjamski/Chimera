## **The Ultimate Development Plan for Project Chimera**

### **Overall Goals**

* **Achieve 95%+ Biological Accuracy:** Ensure the simulation mirrors real-world cannabis cultivation, as per the game's vision.  
* **Ensure Scalability & Performance:** Support 1000+ plants and large facilities at a stable 60 FPS.  
* **Reach Production Readiness:** Achieve over 80% test coverage, eliminate all critical anti-patterns, and establish a maintainable, unified architecture.  
* **Estimated MVP Timeline:** 9-11 months.

---

### **Phase 0: Triage & Foundation (Weeks 1-2)**

**Focus:** Immediately stabilize the codebase by fixing critical anti-patterns and establishing quality gates to prevent further technical debt. This phase is about stopping the bleeding before rebuilding.

**Actions:**

1. **Fix ChimeraManager God Object:** This is the highest priority. Refactor the base class by removing all domain-specific properties (e.g., PlayerFunds). Create and enforce the use of strongly-typed interfaces (ICultivationManager, IGeneticsManager, etc.) to restore type safety and adhere to the Single Responsibility Principle.  
2. **Unify Dependency Injection (DI):** Standardize on the ServiceContainer as the sole DI provider. Systematically remove and replace all **160+ FindObjectOfType calls** and the parallel ServiceLocator and DependencyInjection namespaces. Enforce constructor injection to make dependencies explicit and the codebase testable.  
3. **Eliminate Dangerous Reflection:** Immediately remove the reflection-based field injection in CultivationManager. This is a critical security and maintenance risk that must be replaced with the now-unified DI container.  
4. **Establish CI/CD & Quality Gates:** Set up automated build pipelines that include static analysis and quality gates. Implement rules to **automatically fail any pull request** that introduces forbidden calls like FindObjectOfType, Resources.Load, or raw Debug.Log.

**Outcome:** A stable codebase with a single, clear dependency injection pattern. All critical, project-breaking anti-patterns are resolved, and automated checks are in place to prevent their reintroduction.

---

### **Phase 1: Core Architectural Refactoring (Weeks 3-6)**

**Focus:** Overhaul the foundational architecture to support a scalable and performant simulation. This phase addresses the primary sources of performance bottlenecks and code fragility.

**Actions:**

1. **Implement a Central Update Bus:** Eradicate the "update-loop sprawl" by migrating the **102+ Update() methods** to a centralized ITickable interface. This bus will manage execution order, priority, and allow systems to be paused or throttled based on game state, directly improving frame-time consistency.  
2. **Overhaul Logging Infrastructure:** Replace all **2,300+ Debug.Log calls** with a centralized diagnostics logger. This system will support log levels, categories, and compile-time stripping (\#if CHIMERA\_DEV\_LOGS) to eliminate logging overhead in release builds.  
3. **Complete Addressables Migration:** Eliminate the legacy Resources folder by migrating the remaining **18 Resources.Load calls** to the Addressables system. This ensures all asset loading is asynchronous and memory-managed, preventing hitches during gameplay.  
4. **Enforce Single Responsibility Principle (SRP):** Refactor massive classes identified in the reviews (PlantStrainSO, ServiceContainer, etc.) into smaller, more focused components. This improves readability, testability, and maintainability across the project.

**Outcome:** A clean, robust, and performant core architecture. Per-frame logic is centrally controlled, logging is standardized, asset loading is modern, and large classes are broken down into manageable pieces.

---

### **Phase 2: Core Gameplay Systems Implementation (Weeks 7-14)**

**Focus:** Complete the three interdependent pillars of the game—Genetics, Cultivation, and Construction—to create a fully functional end-to-end gameplay loop.

**Actions:**

1. **Genetics & Breeding System:**  
   * **Make the Blockchain Decision:** Based on the unanimous recommendation from all reviews, **replace the "invisible blockchain" concept with a server-authoritative ledger**. This provides the desired strain uniqueness and trading security with significantly less risk and development overhead.  
   * **Prototype & Implement Fractal Math:** Isolate and prototype the fractal mathematics and deterministic PRNG to ensure the results are stable and performant before full integration.  
   * **Build the C\# Layer:** Implement the C\# managers to interface with the TraitExpressionCompute.shader, manage the seed bank, and handle breeding logic.  
   * **Implement Core Features:** Add the missing **Tissue Culture** and **Micropropagation** mechanics as described in the gameplay document.  
2. **Cultivation & Environment System:**  
   * **Achieve 95% Biological Accuracy:** Replace simplified linear calculations with AnimationCurves and research-based formulas to accurately model plant responses and Genotype × Environment (GxE) interactions.  
   * **Optimize for Scale:** Implement the C\# Jobs System and Burst Compiler for parallel plant updates to ensure the simulation runs smoothly with 1000+ plants.  
   * **Complete Offline Progression:** Implement a chunked simulation process for offline progress, providing the player with a summary UI on return to avoid application freezes.  
3. **Construction System:**  
   * **Strengthen Pillar Integration:** Ensure construction choices (e.g., HVAC placement, insulation) have a meaningful and calculated impact on the Cultivation and Genetics systems.  
   * **Optimize Performance:** Implement object pooling for construction items and mesh combining for large static structures to reduce draw calls.

**Outcome:** The three core gameplay pillars are complete, fully interdependent, and performant, enabling a testable and engaging end-to-end player experience.

---

### **Phase 3: Game Features & Player Experience (Weeks 15-22)**

**Focus:** Build out the surrounding game systems that drive player progression, interaction, and long-term engagement.

**Actions:**

1. **Implement Progression Systems:**  
   * **Skill Tree ("Progression Leaf"):** Build the skill tree using a data-driven approach with ScriptableObjects, allowing for easy tuning by designers.  
   * **Achievements:** Create an event-driven achievement system that tracks player milestones across all save files.  
2. **Harden Economy & Marketplace:**  
   * **Tunable Economy:** Ensure all economic variables are controlled by ScriptableObjects.  
   * **Secure Marketplace:** Implement server-authoritative validation for all transactions involving genetics and schematics to prevent cheating.  
3. **Optimize UI/UX & Add Accessibility:**  
   * **Event-Driven UI:** Eliminate per-frame polling in UI elements, moving to an event-driven update model to reduce CPU overhead.  
   * **Accessibility:** Implement core accessibility features like screen reader support and colorblind modes.  
4. **Solidify Save/Load System:** Implement save file versioning and a migration system to ensure that future game updates do not break existing player saves. Add authenticated encryption (AEAD) to secure save files.

**Outcome:** A complete game loop with robust progression, a secure economy, a polished user experience, and a future-proof save system.

---

### **Phase 4: Quality, Performance & Release Prep (Weeks 23-30)**

**Focus:** A final, dedicated push to ensure the game is stable, highly polished, and meets all performance targets before release.

**Actions:**

1. **Comprehensive Testing:**  
   * **Unit & Integration Tests:** Write tests for critical algorithms (genetics determinism, economic calculations) and system interactions to achieve **\>80% code coverage**.  
   * **Performance Benchmarking:** Create automated performance tests to run in CI, ensuring that large-scale simulations (1000+ plants) consistently meet the 60 FPS target.  
2. **Memory & GC Optimization:** Profile the entire application to identify and fix memory leaks, optimize asset memory usage, and replace any remaining manual garbage collection calls with a smarter, non-blocking strategy.  
3. **Final Polish & Documentation:** Resolve all remaining TODOs in the codebase. Create comprehensive documentation for system architecture and an onboarding guide for new developers to ensure long-term maintainability.

**Outcome:** A stable, performant, and well-documented MVP release candidate that meets all technical and quality success criteria.

---

### **Phase 5: Future-Proofing & Expansion (Post-MVP)**

**Focus:** Lay the groundwork for future updates and expansions outlined in the game's vision.

**Actions:**

* **Design Advanced Systems:** Create detailed technical design documents for post-launch features, including:  
  * Employee and Business Management.  
  * Multiplayer Elements.  
  * A comprehensive Modding Support API.

**Outcome:** A clear, actionable roadmap for post-release content that aligns with the established architecture.