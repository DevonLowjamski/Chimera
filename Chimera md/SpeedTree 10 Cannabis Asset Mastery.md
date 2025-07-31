# **Comprehensive Mastery of SpeedTree 10 for Procedural Cannabis Plant Asset Generation and Unreal Engine Integration in "Project Chimera"**

## **I. Objective & Scope**

This report provides an exhaustive investigation into SpeedTree 10, targeting an expert-level understanding of its features, tools, underlying concepts, and workflows. The primary objectives encompass a deep dive into SpeedTree 10's capabilities, from foundational principles to advanced techniques. A specific focus is placed on the optimal utilization of SpeedTree 10 for the procedural generation of diverse and realistic cannabis plant assets, covering all growth stages and phenotypic variations pertinent to "Project Chimera." Furthermore, this document thoroughly details the integration pipeline between SpeedTree 10 and Unreal Engine, including best practices for achieving optimal performance and visual fidelity. Finally, it catalogues significant learning resources to facilitate the mastery of SpeedTree 10 for these purposes. The intention is for this report to serve as a primary knowledge base, enabling both human developers and AI agents to achieve expertise in SpeedTree 10, capable of understanding complex queries and generating specific tasks or solutions within the SpeedTree environment.

## **II. Core SpeedTree 10 Proficiency (Foundational to Advanced)**

Achieving proficiency in SpeedTree 10 requires a comprehensive understanding of its various editions, installation, interface, core philosophies, and a wide array of modeling techniques, from basic to highly advanced.

### **A. Introduction and Setup**

#### **1\. Overview of SpeedTree 10 versions (Games, Cinema, Indie), licensing, and system requirements.**

SpeedTree 10 has consolidated its offerings, integrating the previously separate Games and Cinema versions into a single installer. This unified approach means a single purchase provides access to all features and export options, streamlining the choice for users. However, different licensing tiers cater to varying user needs and revenue scales:

* **SpeedTree: Learning Edition:** A free version intended for users wishing to learn the software. It allows model creation and saving (in a proprietary.SPL format not openable by other editions) but does not permit mesh exports. It requires a SpeedTree account and internet access. Functionality is otherwise unrestricted, providing full access to SpeedTree's features for educational purposes.  
* **SpeedTree: Indie:** Aimed at hobbyists and independent developers with combined annual revenue or funding below $200,000 USD. This is a subscription-based license (monthly or yearly) activated via SpeedTree store credentials and requires internet access. It allows use on one machine at a time and offers full export capabilities for both games and VFX pipelines.  
* **SpeedTree: Pro:** Designed for professional individuals or studios with annual revenue or funding between $200,000 USD and $1,000,000 USD. It is available as a 6-month or yearly license, with node-locked or floating license options. Internet connection is not continuously required for Pro licenses after activation. This version includes all AAA and VFX features and supports a wide range of export formats to game engines and DCC applications. An evaluation license for a 10-day free trial is also available.  
* **SpeedTree: Enterprise:** For studios or projects with revenue or funding exceeding $1,000,000 USD. This is typically licensed per project and requires direct contact with SpeedTree for custom arrangements. It includes the Modeler, optional access to Games and Cinema libraries, and the Runtime SDK. The Pipeline SDK is an Enterprise-exclusive solution.

**Licensing Model Summary:**

| License Tier | Revenue/Funding Limit | Cost | Export Capabilities | Key Features/Notes |
| :---- | :---- | :---- | :---- | :---- |
| Learning Edition | \< $1M USD | Free | None | Full features for learning;.SPL format; internet required. |
| Indie | \< $200k USD | $19+/month | Full | Subscription; internet required; one machine at a time. |
| Pro | \< $1M USD | $299+ | Full | Term licenses; node-locked/floating; no continuous internet needed. |
| Enterprise | \> $1M USD | Custom | Full | Per project; includes Pipeline SDK access. |

*Table 1: SpeedTree 10 Licensing Tiers and Key Characteristics.*  
**System Requirements:** The minimum specifications for running SpeedTree 10 Modeler are generally consistent across versions:

* **Platform:** Windows, Mac, Linux. SpeedTree 10.0 brought full Linux support to the Runtime SDK.  
* **RAM:** 8 GB.  
* **Storage:** 500 MB for installation.  
* **Graphics Card:** Shader Model 3.0 compatible graphics card.

**Export Options:** SpeedTree 10 offers extensive export capabilities, particularly with the Pro and Enterprise licenses. Common formats include.ST and.ST9 (for Unreal Engine and Unity),.FBX,.OBJ,.USD,.ABC (Alembic, typically for Cinema), and.XML. The specific set of available export formats can depend on whether the workflow is targeted for games or cinema, although the unified Modeler now provides access to both pipelines.

#### **2\. Installation process and initial software setup.**

The installation process for SpeedTree 10 begins with downloading the Modeler from the official SpeedTree store. Once downloaded, the installation follows standard procedures for the respective operating system (Windows, macOS, or Linux).  
Initial software setup primarily involves license activation :

* **Learning Edition:** Activated by logging in with a SpeedTree Store account via the "Help \-\> Licensing info..." menu.  
* **Indie License:** Activated similarly to the Learning Edition, by logging in with SpeedTree Store credentials. Only one machine can be active at a time.  
* **Pro/Enterprise License (Node-Locked):** After purchase, a license key must be requested from Interactive Data Visualization (IDV), the developers of SpeedTree. This involves submitting machine-specific information (Host ID). Once the license string is received via email, it is pasted into the "Pro/Enterprise" section of the "Licensing info..." dialog.  
* **Pro/Enterprise License (Floating):** This involves setting up a Reprise License Manager (RLM) server. The RLM Admin Bundle is downloaded from Reprise Software, installed on a server machine, and license files (.lic and.set configuration file from SpeedTree) are deployed to the RLM installation folder. The RLM server is then activated. Client machines connect to this server for licenses.  
* **Evaluation/Trial:** Upon first run without a license, the Modeler typically enters a trial mode for a limited period (e.g., 10 days for Pro evaluation, or a 30-day evaluation prompt). Trial versions may have limitations, such as random polygons removed from exported meshes or limited animation frames.

Once licensed, users can begin exploring the software, often starting with sample models provided with the application.

#### **3\. In-depth tour of the SpeedTree Modeler interface: all windows, toolbars, menus, and navigation controls.**

The SpeedTree Modeler interface is a sophisticated environment designed for both procedural generation and detailed artistic control. A comprehensive understanding of its components is crucial for efficient workflow. The official SpeedTree 10 documentation map and introductory videos provide visual and textual guides.  
**Key Interface Components:**

* **Tree Window (3D Viewport):** This is the primary window for visualizing and interacting with the 3D model.  
  * **Navigation:** Standard 3D navigation controls (orbit, pan, zoom) are used. Specifics can be found in "Tree Window navigation".  
  * **Toolbar:** Contains tools for selection modes (Generator vs. Node), visibility toggles (e.g., for wireframe, collision primitives, hints), rendering modes, force manipulation, and snapping. The "Edit" group on the toolbar, for example, controls the switch between Generator and Node edit modes.  
  * **Overlays:** Provides visual feedback for various elements like wind, LODs, and selection highlighting.  
  * **Properties:** Contextual properties related to the viewport itself or selected scene elements.  
* **Generation Editor:** This window displays the hierarchical structure of the plant model, composed of interconnected generators.  
  * **Function:** Users add, link, and organize generators here to define the plant's structure (e.g., linking a Branch generator to a Trunk generator).  
  * **Toolbar:** Includes buttons for adding, deleting, duplicating, and arranging generators, as well as randomizing selections.  
  * **Overlays:** Icons on generators can indicate their type (e.g., hand-drawn ), if a node edit has occurred , or if a force is active.  
* **Property Bar (Property Editor):** This is where the parameters of selected generators, nodes, forces, or other objects are displayed and edited.  
  * **Organization:** Properties are grouped into categories (e.g., "Generation," "Spine," "Skin," "Forces") and can be numeric, boolean, string/list-based, or curve-based.  
  * **Controls:** Include numerical input fields, sliders, checkboxes, dropdown lists, and curve thumbnails that open the Curve Editor.  
  * **Variance Controls:** Buttons next to numeric properties allow editing of variance values, introducing randomization.  
* **Curve Editor:** Accessed by clicking on a curve thumbnail in the Property Bar, this editor allows for detailed shaping of property profiles and distributions.  
  * **Function:** Curves control how property values change across ranges (e.g., along a branch length, or from base to tip of a tree).  
  * **Interface:** Displays the curve, control points, and handles. Provides presets and tools for manipulating curve shapes (e.g., Bézier handles).  
* **Variance Editor:** Accessed via the variance button next to a property, this allows fine-tuning of how much a property's value can deviate randomly.  
* **Assets Bar:** A collection of tabs for managing materials, meshes, displacements, and masks.  
  * **Materials Bar:** Manages material assets, texture assignments, and PBR properties. Includes the Map Editor, Color Editor, and UV Area Editor. The "Generate Mesh" tool can be found here for creating meshes from textures.  
  * **Material Sets Bar:** Allows grouping of materials for easier assignment and management.  
  * **Meshes Bar:** Manages imported or generated mesh assets (e.g., for leaves, fronds, props). Includes the Cutout Editor for creating meshes from textures with alpha.  
  * **Displacement Bar:** Manages displacement assets.  
  * **Masks Bar:** Manages mask assets used to control growth or material application.  
* **Main Menu Bar:** Standard application menu (File, Edit, Window, Tools, Help, etc.) providing access to all software functions, including saving/loading, export options, tool invocation (e.g., "Generate Collision Primitives" ), preferences, and licensing information.  
  * **Window Menu:** Allows toggling the visibility of different interface panes and resetting the layout. Theme selection (dark/light) is also available here.  
  * **Tools Menu:** Contains utilities like "Arbitrary Scale" , "Create mesh asset from selection" , and "Create collection from selection".  
* **Timeline Bar:** Used for managing animations, including Growth animations and Season changes.  
* **Rules Window:** Interface for creating and managing Lua-based "Rules" to customize model parameters.  
* **Message System/Output Window:** Displays errors, warnings, and informational messages.

The interface is customizable, allowing windows to be detached, docked, and tabbed. Resetting the layout to its default state is possible via the "Window" menu.

### **B. Fundamental Concepts**

SpeedTree's power lies in its procedural approach, built upon a set of core components and concepts that enable the creation of complex and varied vegetation.

#### **1\. The procedural generation philosophy of SpeedTree.**

SpeedTree's modeling philosophy is a hybrid approach, blending robust procedural generation with direct artistic control through hand-drawing and node editing. Vegetation, being inherently complex and diverse, benefits significantly from procedural methods which can efficiently generate intricate structures based on rules and parameters. This allows artists to quickly establish base forms and overall characteristics.  
The procedural system is built on a hierarchy of "Generators" that create "Nodes" (the actual geometric components like branches or leaves). Artists define the behavior of these generators by adjusting their "Properties," which include numerical values, boolean toggles, and, crucially, "Curves" that dictate how values are distributed. Variance can be introduced to most properties, allowing for natural-looking randomization and unique instances.  
Once a base model is procedurally defined, SpeedTree allows for fine-grained artistic intervention. Individual nodes can be selected and their properties offset, or their geometry directly manipulated (e.g., moving a branch, deleting a leaf). Hand-drawing tools further empower artists to sculpt specific forms or add unique elements that might be difficult to achieve purely procedurally. This combination ensures that artists are not limited by purely algorithmic results but can guide the procedural system and then refine its output to match their vision. SpeedTree 10 continues to enhance this philosophy by adding features like physics-based vines and precision pruning tools, further empowering iteration and art direction.  
The goal is to leverage proceduralism for the heavy lifting of creating complex organic structures while providing intuitive tools for artistic refinement and control.

#### **2\. Core components: Generators (e.g., Trunk, Branch, Leaf Frond, Zone of Influence), Nodes, and their hierarchical relationships.**

* **Generators:** These are the fundamental building blocks in SpeedTree, acting as rule sets or blueprints that define how specific parts of a plant are created and behave. Each generator type is specialized for creating a certain kind of geometry or effect. They are organized hierarchically in the Generation Editor, where child generators inherit properties and context from their parents.  
  * **Tree Generator:** Every SpeedTree model has exactly one Tree Generator at the root of the hierarchy. It controls global settings for the tree, such as overall size scaling, wind parameters, level of detail (LOD) behavior, and seasonal changes.  
  * **Spine Generator (Trunk/Branch):** This is a versatile generator responsible for creating the main structural elements like trunks, branches, and roots. It can also be used for hand-drawn branches. Its icon changes based on whether it's configured for branches, fronds, or both. Key properties control length, radius, segmentation, branching patterns (phyllotaxy, bifurcation), and response to forces.  
  * **Leaf Mesh Generator / Leaf Generator:** Responsible for placing leaf geometry, which can be either 2D cards (billboards) or custom 3D meshes. Properties control leaf size, count, orientation, and material assignment. The "Batched Leaf Generator" is an optimized version for VFX trees with very high leaf counts, forgoing individual node editing for faster computation.  
  * **Frond Generator:** Creates 2-sided frond geometry along a spine, often used for palm leaves, ferns, or other planar structures. Mesh Fronds allow custom meshes to be used instead of standard planar fronds, useful for flowers, fruit, or complex leaves.  
  * **Zone Generator (Zone of Influence):** Defines a 3D region (disk or mesh-based) from which other generators can spawn children. This is useful for creating clusters of elements, populating surfaces, or controlling growth within a specific volume. Properties include radius (for disk zones), mesh assignment, and how children are distributed within the zone (e.g., Area Influence, Adapt to Masks ).  
  * **Other Specialized Generators:** SpeedTree includes many other generators for specific effects:  
    * **Cap Generator:** Closes off open-ended branches.  
    * **Knot Generator:** Creates lumps, knots, or cavities on branches.  
    * **Fin Generator:** Creates thin flaps of geometry (e.g., fungus, moss).  
    * **Shell Generator:** Wraps a parent branch with another layer of geometry (e.g., moss, low-poly vines).  
    * **Vine Generator:** New in SpeedTree 10, for creating physics-based hanging or crawling vines.  
    * **Decal Generator:** Wraps a material and flat mesh onto a parent for surface details.  
    * **Mesh Generator:** Allows imported mesh assets to be used as part of the model.  
    * **Mesh Converter Generator:** Tools for creating tiling maps and procedural geometry extensions on meshes, often used in photogrammetry workflows.  
    * **Reference Generator:** Allows referencing and instancing other generator setups to declutter the graph and edit multiple instances via a single source.  
    * The hierarchical linking of these generators in the Generation Editor defines the plant's fundamental structure. For instance, a Leaf Mesh generator is typically linked as a child to a Branch generator, meaning leaves will sprout from the branches created by that parent generator.  
* **Nodes:** These are the individual instances of geometry created by a generator. For example, if a Branch generator has its "Frequency" property set to 10, it will create 10 branch nodes.  
  * **Types of Nodes:** Spine nodes (for branches/fronds), Leaf nodes (cards or meshes), Zone nodes, and Proxy nodes.  
  * **Hierarchical Relationship:** Nodes are children of their creating generator and inherit its base properties. They are also part of the overall plant hierarchy (e.g., a leaf node is attached to a specific branch node).  
  * **Individual Editing:** A key aspect of SpeedTree's workflow is the ability to switch to "Node Edit Mode" (often via the TAB key or a toolbar button) and select individual nodes. Once a node is selected, its properties can be modified in the Property Editor. These modifications are *offsets* to the values computed by the parent generator. This allows for fine-tuning specific parts of the plant without losing the underlying procedural control. For example, an artist can make one specific branch longer or change its angle. If the parent generator's length is later globally increased, the individually edited branch will still maintain its relative length offset. Nodes can also be deleted.

The interplay between hierarchically linked generators defining broad rules, and the ability to make specific edits to the resulting nodes, forms the core of SpeedTree's procedural yet art-directable modeling paradigm.

#### **3\. Properties: Understanding numeric, boolean, curve, and variance controls for each generator and node.**

Properties are the parameters that control the behavior of generators and the characteristics of the nodes they create. They are displayed and edited in the Property Editor when a generator or node is selected.

* **Numeric Properties:** These control quantifiable attributes like length, radius, count, angle, or intensity. They are typically edited via input fields or sliders. Examples include Spine:Length, Generation:Frequency, Leaf:Size.  
* **Boolean Properties:** These are on/off switches, typically represented by checkboxes. They enable or disable features or behaviors. Examples include Forces:Allow Forces, Geometry Types:Frond, Spine:Limit Length.  
* **String/List Properties:** Some properties involve selecting from a list of options (e.g., a dropdown menu for Generation:Mode which might include "Interval", "Phyllotaxy", etc.) or involve text input (though less common for core modeling parameters). Material assignments often involve selecting a named material from a list.  
* **Curve Properties:** Many numeric properties are associated with one or more curves, displayed as thumbnails next to the property value. Clicking a curve thumbnail opens the Curve Editor, allowing detailed control over how the property's value is distributed.  
  * **Function:** Curves act as multipliers or profiles for the base property value, allowing it to change along the length of a parent (Parent Curve), along the length of the node itself (Profile Curve), or based on other factors like LOD or distribution density.  
* **Variance Controls:** Most numeric properties have an associated variance control, typically a small button or field next to the main value.  
  * **Function:** Variance introduces randomization to the property's value for each generated node. For example, if Spine:Length is 10.0 and its variance is 2.0, individual branches generated will have lengths ranging randomly between 8.0 and 12.0. A variance of 0.0 means no randomization for that specific property.  
  * **Editing:** Clicking the variance button often switches the input field to accept the variance amount or opens a dedicated Variance Editor.  
  * **Impact:** Variance is crucial for creating natural-looking plants, as no two real-world branches or leaves are perfectly identical. It's a key component of SpeedTree's ability to generate unique plant instances from the same base rules.

When editing properties for an individual **Node** (in Node Edit Mode), the Property Editor displays *offset* values for these properties. A default node shows '0' for numeric offsets, meaning it uses the generator's computed value directly. Any change made to a node's property is an addition or subtraction from the generator's value for that specific node. This non-destructive offset system is fundamental to SpeedTree's blend of procedural and manual control.

#### **4\. Profile Curves: How they define shapes and distributions.**

Profile Curves are a critical type of curve in SpeedTree, typically colored cyan in the interface, that define how a property's value is applied along the length of each individual node created by a generator. They affect each node within that generator uniformly.

* **Function:** Imagine a branch. A Profile Curve associated with its Skin:Radius property would determine the branch's thickness profile from its base to its tip. The horizontal axis (X-axis, typically 0 to 1\) of the curve represents the normalized length of the node (0 \= base, 1 \= tip). The vertical axis (Y-axis) represents a multiplier (often 0 to 1, but can exceed 1\) for the property's base value.  
  * For example, a Skin:Radius Profile Curve that starts at Y=1 at X=0 and linearly decreases to Y=0 at X=1 would create a branch that tapers from its full base radius to a point at its tip.  
* **Applications:**  
  * **Shape Definition:** Defining the tapering of trunks and branches (radius), the shape of fronds (width, height along the frond's spine). The Frond:Shape:Contour property uses a profile curve to define the frond's outline.  
  * **Value Distribution:** Controlling how forces are applied along a branch, how much "curl" or "twist" is applied at different points along a spine, or how leaf size/orientation changes from the base to the tip of a branch segment they are attached to.  
  * **Segment Distribution:** Profile curves can influence the distribution of radial or length segments along a spine, though this is often also controlled by absolute segment counts.  
* **Interaction with Parent Curves:** Profile Curves work in conjunction with Parent Curves (green curves). Parent Curves determine the base value of a property for a node based on its position along its *parent* structure (e.g., branches higher up a trunk might be shorter). The Profile Curve then takes that determined base value and modifies it along the length of the *individual node itself*.  
* **Editing:** Profile Curves are edited in the Curve Editor, accessed by clicking their thumbnail in the Property Bar. The editor provides tools to add, move, and delete control points, adjust tangents (including Bézier handles), and use presets for common shapes (e.g., linear, ease-in/out, bell curve).  
* **Node Offsets:** When editing an individual node, an *offset* Profile Curve can be applied. This offset curve is added to (or subtracted from) the generator's computed Profile Curve for that specific node, allowing for unique shaping of individual elements.

The effective use of Profile Curves is essential for achieving nuanced and realistic shapes in SpeedTree models, allowing artists to move beyond simple uniform values and define detailed variations along the length of plant components. SpeedTree 10 also introduced "Absolute Curves" for precise control on extensions, which relates to how these profiles can be applied.

#### **5\. Spine-based modeling: The concept of spines and their influence on growth.**

Spine-based modeling is a foundational concept in SpeedTree. A "spine" is essentially the central invisible line or curve that defines the core path and orientation of structural elements like trunks, branches, and fronds. The geometry (the visible mesh) is then "skinned" around this spine.

* **Role of Spines:**  
  * **Growth Direction and Path:** The spine dictates the primary direction and curvature of a branch or trunk. Properties like Spine:Angle, Spine:Disturbance, Forces (Gravity, Phototropism, etc.), and hand-drawing directly manipulate the shape of this underlying spine.  
  * **Length and Segmentation:** The length of the spine determines the length of the branch. The number of length segments in the visible mesh is often distributed along this spine.  
  * **Attachment Points:** Child elements (like other branches or leaves) are generated at positions along their parent's spine, controlled by properties like Generation:First and Generation:Last.  
  * **Profile Application:** Profile Curves (e.g., for radius) are evaluated along the length of the spine, from its base to its tip.  
* **Spine Generators:** The "Spine Generator" is the primary tool for creating these elements. It has numerous properties to control the spine's characteristics:  
  * Spine:Length: Determines the length of the spine.  
  * Spine:Angle: Controls the initial angle relative to the parent.  
  * Spine:Disturbance, Spine:Noise, Spine:Jink: Introduce various forms of randomness and perturbation to the spine's path, making it look more organic.  
  * Spine:Accuracy: Controls the fidelity or number of points in the computed spine, affecting smoothness and detail.  
  * Properties related to bifurcation (splitting) and breaking also act upon the spine structure.  
* **Influence on Growth:**  
  * The spine is the armature upon which all other details of a branch are built. Its shape directly influences the final silhouette and posture of the branch.  
  * Forces applied to a generator act by deforming these spines. For example, a wind force will bend the spines, and the mesh skinned to them will follow.  
  * Hand-drawing directly creates and manipulates these spines using Bézier curves or control points.  
* **Mesh Spines (SpeedTree 10 Feature):** A significant advancement in SpeedTree 10 is the "Mesh Spines" system. This allows users to rig imported static meshes (e.g., from photogrammetry or other DCCs) with procedural spines.  
  * **Function:** This means an existing mesh can be imbued with SpeedTree's procedural capabilities. Spines can be defined within or along the mesh, and then SpeedTree can:  
    * Grow new procedural geometry (branches, leaves) off these spines on the mesh.  
    * Apply SpeedTree wind animations to the mesh by deforming these spines.  
    * Export this rigged mesh with valuable information for advanced animation or breaking in other software.  
  * This bridges the gap between purely static imported meshes and fully procedural SpeedTree geometry, offering a powerful workflow for integrating custom-modeled assets or scans while retaining procedural control and dynamic behavior. The "Mesh Helper" tool is used to add spines to hero meshes.

Understanding that almost all structural elements in SpeedTree are built around these deformable spines is key to mastering how properties and forces shape the final plant model.

### **C. Basic to Intermediate Modeling Workflow**

This section outlines the typical steps and considerations when creating plant assets in SpeedTree, covering fundamental techniques and common practices.

#### **1\. Creating a plant from scratch: Step-by-step process.**

Creating a plant from scratch in SpeedTree involves a systematic process of building up the structure using generators and refining their properties. While specific tutorials provide visual walkthroughs , a general workflow can be outlined:

1. **Start with a Template or Blank Scene:**  
   * Initiate a new SpeedTree file. Users can start from a blank scene or use one of the provided templates (e.g., "Blank," "Games Optimized," "VFX Optimized") as a starting point. Templates often provide a basic generator setup.  
2. **Establish the Trunk:**  
   * The **Tree Generator** is the root. Add a **Spine Generator** as a child to the Tree Generator to represent the main trunk.  
   * Adjust trunk properties: Spine:Length, Skin:Radius (using profile curves for tapering), Segments (for mesh resolution).  
   * Apply a basic bark material from the **Materials Bar**.  
3. **Add Primary Branches:**  
   * Add another **Spine Generator** as a child to the Trunk generator. This will create the first level of branches.  
   * Control branch placement using Generation properties:  
     * Mode (e.g., "Interval," "Phyllotaxy").  
     * Frequency (number of branches).  
     * First and Last (range along the parent trunk where branches appear).  
   * Adjust branch characteristics: Spine:Length, Skin:Radius, Spine:Angle (initial angle from trunk), Spine:Disturbance (for natural variation). Use Parent Curves to vary these properties along the trunk's height (e.g., shorter branches near the top).  
4. **Add Secondary and Tertiary Branches (Iterative Process):**  
   * Repeat the process by adding new Spine Generators as children to the primary branches, and then to the secondary branches, to create finer branching structures.  
   * Adjust properties at each level to achieve desired density, length, and angle variations. Parent curves become increasingly important for controlling how child branches behave based on their position on their immediate parent.  
5. **Add Leaves/Fronds:**  
   * Select the branch generators where leaves should appear (typically the finest branches).  
   * Add a **Leaf Mesh Generator** (for custom meshes or cards) or a **Frond Generator** (for frond-like structures) as a child.  
   * Import or create leaf/frond meshes in the **Meshes Bar** and assign appropriate materials (with textures for albedo, normal, opacity, SSS) in the **Materials Bar**.  
   * Adjust Leaf:Size, Generation:Frequency, Leaf:Orientation properties (e.g., Sky Sensitivity, Surface Adhesion).  
6. **Refine with Curves and Variance:**  
   * Throughout the process, extensively use **Profile Curves** to shape elements (e.g., branch tapering) and **Parent Curves** to control property distribution based on hierarchy (e.g., leaf size decreasing towards branch tips).  
   * Apply **Variance** to key properties (length, angle, size, count) to introduce natural randomness and avoid uniformity.  
7. **Incorporate Forces (Optional but Recommended):**  
   * Add **Forces** like Gravity (to make branches sag realistically), Phototropism (to make parts grow towards light), or Obstacles/Attractors to further shape the plant.  
8. **Hand-Drawing and Node Editing for Final Touches:**  
   * Switch to **Node Edit Mode** to select and manually adjust individual branches or leaves that are out of place or require specific artistic tweaks.  
   * Use **Hand-Drawing** tools if specific, unique branch shapes are needed that are difficult to achieve procedurally.  
9. **Optimization (LODs, Segments):**  
   * Adjust segment counts (Segments:Length, Segments:Radial) on Spine Generators to balance detail and performance.  
   * Set up Levels of Detail (LODs) if the asset is for real-time use.  
10. **Review and Iterate:**  
    * Constantly review the model from different angles and distances. Use randomization (sparingly at later stages if manual edits are present) to check how robust the procedural setup is.  
    * Iterate on generator properties, curves, and manual edits until the desired result is achieved.

This iterative process, moving from broad procedural strokes to fine-tuned details, is central to the SpeedTree workflow.

#### **2\. Hand-drawing vs. Procedural generation techniques and when to use each.**

SpeedTree offers a powerful combination of procedural generation and hand-drawing tools, allowing artists to choose the best approach for different aspects of a model or at different stages of the creation process.

* **Procedural Generation:**  
  * **Strengths:**  
    * **Efficiency:** Quickly generates complex structures (e.g., entire branching systems, thousands of leaves) based on rules and parameters. Ideal for creating the bulk of the plant.  
    * **Randomization & Variation:** Easily create numerous unique variations of a plant by adjusting random seeds and variance parameters.  
    * **Non-Destructive Iteration:** Changes to generator properties or curves globally affect all generated elements, allowing for rapid iteration on the overall form.  
    * **Control via Parameters:** Precise control over attributes like density, distribution, angles, and lengths through numerical inputs and curves.  
  * **When to Use:**  
    * Establishing the main structure of the plant (trunk, primary and secondary branching).  
    * Populating large numbers of elements like leaves, twigs, or small repeating details.  
    * Creating natural-looking randomness and variation across the plant or between multiple instances.  
    * When needing to quickly experiment with different overall shapes and growth patterns.  
    * For elements that follow predictable botanical rules (e.g., phyllotaxy, apical dominance effects).  
* **Hand-Drawing Techniques:**  
  * **Strengths:**  
    * **Artistic Control:** Provides direct, intuitive control over the shape and placement of individual branches or spines. Artists can draw paths with a mouse or tablet.  
    * **Unique Forms:** Ideal for creating specific, characteristic shapes that might be difficult or overly complex to achieve purely procedurally (e.g., a uniquely gnarled hero branch, a specific bend in a trunk).  
    * **Refinement:** After procedural generation, hand-drawing can be used to add or modify key branches to perfect the silhouette or composition. Hand-drawn branches can then have procedural elements grow off them.  
  * **Process:** Typically involves selecting a Hand Draw tool, holding a key (e.g., Spacebar), and clicking/dragging on an existing part of the tree or the ground to create a new branch/spine. The drawn path is converted into a Bézier spline whose control points can be further edited.  
  * **When to Use:**  
    * Creating "hero" branches or trunks with very specific, art-directed shapes.  
    * Adding unique details or correcting specific areas after initial procedural generation.  
    * Situations where precise placement and curvature are paramount and difficult to achieve with global parameters.  
    * Modeling elements that don't follow strict procedural rules, like a fallen log or a uniquely broken branch.  
* **Combining Techniques:** The true power of SpeedTree lies in the seamless integration of these two approaches. A common workflow is:  
  1. Use procedural generation to create the overall structure and distribution of elements.  
  2. Convert specific procedural branches to hand-drawn if they require significant custom shaping, or add new hand-drawn branches where needed. The "Convert to Hand Drawn" button on a generator or an option in the Generation Editor facilitates this.  
  3. Procedural elements (like smaller branches or leaves) can then be grown off these hand-drawn branches.  
  4. Node editing provides the final layer of tweaking for individual procedural or hand-drawn elements.

**Pro-Tips & Common Pitfalls:**

* **Procedural First:** It's generally advisable to get as close as possible with procedural tools before resorting to extensive hand-drawing or node editing, as these manual edits can be lost if the parent generator is significantly changed or randomized.  
* **Hand-Drawn Templates:** Hand-drawn generators can be saved as templates (.STT files) for reuse.  
* **Limitations:** Hand-drawn generators don't use "Generation" parameters like frequency; each node is added manually. Bifurcation is also typically disabled for hand-drawn branches.  
* **Performance:** Converting a generator with a massive number of nodes (e.g., 100,000 twigs) to hand-drawn would be computationally prohibitive.

By understanding the strengths of each method, artists can efficiently create highly detailed and art-directable vegetation.

#### **3\. Mesh generation: Understanding resolution, segments, and optimization.**

SpeedTree provides tools for generating and managing mesh geometry, which is crucial for balancing visual fidelity with performance, especially for real-time applications.

* **Mesh Components:**  
  * **Branches/Trunks (Spines):** The geometry for these is procedurally generated by "skinning" a mesh around the underlying spine. Key properties influencing this mesh are:  
    * **Segments:Length:** Determines the number of divisions along the length of the spine. More segments result in smoother curves but higher polygon counts.  
    * **Segments:Radial:** Determines the number of sides around the circumference of the branch. Higher values create rounder branches but increase polygons.  
    * **Optimization Properties:** Generators often have "Optimization" properties that can intelligently reduce segments where they have less visual impact.  
  * **Leaves/Fronds:** These can use simple planar cards or custom 3D meshes.  
    * **Mesh Cutouts:** For card-based leaves, the **Cutout Editor** (accessed via the Materials Bar or Meshes Bar) allows creating an optimized 2D mesh that tightly fits the opaque areas of a leaf texture. This minimizes overdraw from transparent areas.  
    * **Custom Meshes:** Users can import.FBX or.OBJ meshes for leaves, fronds, flowers, fruit, etc.. These are managed in the **Meshes Bar**.  
    * **Generate Mesh Tool:** Found in the Material Assets bar, this tool can automatically create a mesh from an imported texture, attempting to fit the opaque areas. It offers "adaptive" or "grid" algorithms.  
* **Resolution (VFX Context):**  
  * For VFX workflows, SpeedTree offers a "Resolution" system with states like High, Medium, Low, and Draft. This allows artists to work with a single model but export it at different polygon counts suitable for varying shot requirements.  
  * **Resolution Curves:** Orange-colored curves associated with segment properties (e.g., Segments:Resolution:Length) control how segment counts are scaled for each resolution state. This is a non-destructive way to manage detail levels.  
  * It is generally recommended to model at "High" resolution and use the other states for export or for accelerating complex computations like wind editing.  
* **Level of Detail (LOD) (Games Context):**  
  * For real-time applications, LODs are critical for performance. SpeedTree can automatically generate multiple LODs for a model.  
  * **Dynamic LOD Properties:** Found on the Tree Generator, these control the number of LOD levels and how transitions occur.  
  * **LOD Transition:** Parts of the tree are intelligently removed or simplified (e.g., branches shrink, leaves are culled or grow to maintain silhouette) as the model moves further from the camera.  
  * **Segment Reduction in LODs:** Branch and frond segment counts can be reduced across LODs, either explicitly or via an "Optimization" property.  
  * **Mesh LODs:** For components using custom meshes (leaves, fronds), different mesh versions (High, Medium, Low detail) can be assigned in the Meshes Bar and swapped out at different LOD levels.  
* **Optimization Strategies:**  
  * **Segment Counts:** Judiciously set length and radial segments. Avoid overly dense meshes where detail isn't visible. Use the "Scribed" view (wireframe) to inspect polygon density.  
  * **Cutouts:** Always use optimized mesh cutouts for card-based foliage to reduce fill rate costs.  
  * **LODs:** Implement and thoroughly test LOD transitions to ensure they are smooth and occur at appropriate distances. Popping can occur if changes are too drastic.  
  * **Texture Atlasing:** While not mesh generation per se, efficient texture atlasing (combining multiple textures into one sheet) is vital for reducing draw calls in game engines and is closely tied to material setup on meshes.  
  * **Mesh Anchors:** A feature in the Cutout Editor that allows precise definition of attachment points for children on low-detail meshes, helping create fuller-looking models with fewer triangles.  
  * **Collections:** Can be used to create complex structures that are then treated as a single mesh, potentially aiding in optimization or specific collision setups.

Understanding and effectively using these mesh generation and optimization tools is fundamental to creating assets that are both visually appealing and performant in their target application.

#### **4\. UV unwrapping and texture coordinate generation within SpeedTree.**

SpeedTree handles UV unwrapping and texture coordinate generation both automatically for its procedural geometry and provides options for managing UVs during export, particularly for game engines and when creating texture atlases.

* **Procedural Geometry UVs (Branches, Trunks):**  
  * For spine-based geometry like trunks and branches, SpeedTree automatically generates UV coordinates. These are typically cylindrical or stretched along the length of the spine to allow tiling bark textures.  
  * **Mapping Properties:** Generators like the Spine Generator have "Mapping" properties (e.g., Mapping:U Correction, Mapping:V Style, Mapping:Tile U/V) that control how textures are applied and tiled across the surface.  
  * **Branch Blending UVs:** For seamless transitions at branch intersections, SpeedTree can generate specialized UV data used by shaders in game engines.  
  * **Detail Textures:** SpeedTree supports a second UV channel for detail maps, allowing for finer surface variations overlaid on the base texture.  
* **Leaf and Frond UVs:**  
  * **Mesh Assets:** If using custom meshes for leaves or fronds, their UVs are typically created in an external modeling package and imported with the mesh. SpeedTree will use these existing UVs.  
  * **Cutout Editor / Generate Mesh:** When creating meshes from textures within SpeedTree (e.g., using the Cutout Editor or Generate Mesh tool), the UVs are generated to map directly to the source texture.  
* **UV Unwrapping for Export (Static Meshes / VFX):**  
  * When exporting static meshes (e.g., for VFX pipelines or baking), SpeedTree offers an "Unwrap UVs" option. This process takes all the different material "chunks" used by the tree and arranges their UVs into a new, unified texture space.  
  * **Layout Options:**  
    * **Atlas:** Packs all UV chunks into a single large texture layout.  
    * **UDIM:** Saves each chunk to its own UV tile (e.g., 1001, 1002), commonly used in VFX.  
  * **Control:** Users can control whether different geometry types (branches, leaves, etc.) or materials have their UVs stacked (sharing UV space, good for tiling textures) or fully unwrapped (each piece gets unique UV space, good for painting or sculpting).  
  * **Scaling and Favoring:** Options exist to control the relative scale of UV islands in the atlas, favoring either on-model surface area or original texture area.  
  * **Use Cases:** Unwrapping everything uniquely is useful for 3D painting or sculpting the entire model. Stacking repeating elements like leaves saves texture space while unwrapping unique parts like the trunk for detailed work is also common.  
* **UV Atlasing for Game Engines (Real-time):**  
  * A critical optimization for game engines is texture atlasing, where multiple smaller textures (e.g., for different leaf types, flowers, small branches) are combined into a single larger texture sheet. This reduces the number of materials and draw calls.  
  * **Export Options:** The game export dialog in SpeedTree provides "Atlas" options :  
    * **None:** No atlasing; materials export with separate textures.  
    * **Non-Wrapping:** Materials are atlassed if their geometry UVs are within the 0-1 range. Wrapping UVs (like on branches) export separately.  
    * **Everything:** Attempts to put all materials into the atlas. Geometry with UVs outside 0-1 will be unwrapped.  
  * **Allow V Wrapping:** A crucial option for branches. Branches often have UVs that tile significantly in the V direction (along the length). Fully unwrapping these can consume vast atlas space or reduce texel density. "Allow V wrapping" places these elements such that their top/bottom edges align with the atlas edges, allowing V-tiling to work correctly within the atlas and preserving resolution.  
  * **Texture Packing:** SpeedTree also performs texture packing, where different material maps (e.g., diffuse alpha, normal, specular) are combined into channels of output textures according to engine requirements or custom setups.

Proper UV management, especially understanding the unwrapping and atlasing options during export, is vital for achieving both visual quality and optimal performance in the target application. For PBR workflows, ensuring correct UV layout for all necessary maps (albedo, normal, roughness, metallic, AO, opacity) is essential.

#### **5\. Materials and Texturing: Applying textures, understanding material parameters within SpeedTree (PBR workflow).**

SpeedTree 10 employs a Physically Based Rendering (PBR) workflow for its materials, ensuring that assets react realistically to light across various environments. Materials are managed in the **Materials Bar** and their properties are edited there or in the associated **Map Editor** and **Color Editor**.

* **Applying Textures:**  
  * Textures (e.g.,.PNG,.TGA,.DDS) are loaded into specific map slots within a material definition in the Materials Bar.  
  * Clicking the texture button for a map slot opens the **Map Editor**, where the texture file can be selected and properties like tiling, offset, and channel usage can be adjusted.  
  * Materials are then assigned to generators (e.g., a bark material to a Trunk generator, a leaf material to a Leaf Mesh generator) either by dragging from the Materials Bar onto the geometry in the Tree Window or via the generator's properties.  
* **PBR Material Parameters (Map Types):** SpeedTree's PBR system uses several standard texture maps:  
  * **Color (Albedo):** Defines the intrinsic base color of the material under neutral white light. For PBR, albedo values should stay within a physically plausible range (not pure black or pure white). SpeedTree provides an "Albedo Check" render mode to help validate these values (red indicates too high, blue too low).  
  * **Opacity (Alpha/Mask):** Controls transparency. White is fully opaque, black is fully transparent. Often stored in the alpha channel of the Color map or as a separate map.  
  * **Normal:** A tangent-space normal map that provides fine surface detail and simulates bumps and crevices without adding polygons. Higher contrast normal maps create more pronounced details.  
  * **Glossiness/Roughness:** These are inversely related and control the microsurface scattering of light.  
    * **Glossiness (SpeedTree 8 terminology):** High gloss results in small, sharp, bright specular highlights. Low gloss results in large, blurry, dull highlights.  
    * **Roughness (Standard PBR term, often used in UE):** High roughness means a diffuse surface; low roughness means a smooth, reflective surface. SpeedTree materials will map to this concept in engine shaders.  
  * **Specular:** Scales the amount and color of light reflected off a surface. For most non-metallic materials (dielectrics like wood, leaves), this is a near-grayscale value (e.g., 0.75 gray is a common default). For metals, the specular color is derived from the Albedo map.  
  * **Metallic:** Defines how "metal-like" a surface is. 0 for non-metals, 1 for metals. Values in between are rare (e.g., corroded metal). Most vegetation materials will have Metallic set to 0\. When Metallic is 1, the Color map defines the specular reflection color, and the base diffuse becomes black.  
  * **Subsurface (SSS) / Translucency:** Simulates light passing through thin or porous materials like leaves and petals.  
    * **Subsurface Color:** The color of the light after passing through the material.  
    * **Subsurface Amount (Subsurface%):** Controls the intensity of the SSS effect.  
    * SpeedTree often keeps these as separate maps for easier editing, though they might be combined in some export formats. A "Subsurface Check" render mode helps validate SSS color values.  
  * **Ambient Occlusion (AO):** Simulates self-shadowing in crevices and occluded areas, darkening ambient light and slightly affecting diffuse light. SpeedTree can also render per-vertex AO; a material AO map provides finer detail. The final AO is often a multiplication of both, so material AO maps should be mostly white to avoid over-darkening.  
  * **Detail Map & Detail Normal Map:** Uses a secondary UV channel to overlay additional texture details (e.g., fine bark texture, leaf veins) that can tile independently of the base maps.  
  * **Custom Map:** A user-defined map slot that isn't used by default SpeedTree render modes but can be utilized for custom shaders or exported for use in game engines.  
* **Material Editor Functionality:**  
  * **Color/Value Inputs:** For each map slot, a base color or scalar value can be set, which is then multiplied by the assigned texture.  
  * **Texture Adjustments:** Within the Map Editor, textures can be tiled, offset, and channels can be remapped or inverted.  
  * **Variations:** The Materials Bar allows for creating color variations of a material, useful for adding subtle diversity to elements like leaves without creating entirely new materials. These variations are baked into new textures on export.  
  * **Two-Sided:** A material property to make it render on both front and back faces, essential for elements like single-plane leaves.

A solid understanding of PBR principles and how SpeedTree implements them through its material system is vital for creating assets that integrate seamlessly and realistically into modern rendering pipelines like Unreal Engine.

### **D. Advanced Modeling Techniques and Features**

Beyond basic procedural construction, SpeedTree 10 offers a suite of advanced tools and features for creating highly detailed, dynamic, and unique vegetation assets.

#### **1\. Forces: Wind, gravity, obstacles, attractors, and their application.**

Forces in SpeedTree are objects that influence the growth direction and shape of spines (trunks, branches, fronds). They provide a powerful way to art-direct plant forms beyond basic generator properties.

* **Adding and Allowing Forces:**  
  * Forces are added to the scene via the Tree Window toolbar or right-click menu.  
  * For a force to affect a generator's output, it must be explicitly enabled for that generator in the "Forces" group of its Property Bar. A master "Allow forces" checkbox also exists.  
* **Types of Forces :**  
  * **Direction:** Spines grow towards the direction the force arrow points. Can simulate phototropism (growing towards light) or general directional pressure.  
  * **Magnet:** Spines converge towards or diverge from a central point of the force.  
  * **Gnarl:** Spines twist around a world-space vector defined by the force's rotation.  
  * **Twist:** Spines twist along their local up-vectors, influenced by the force's rotation.  
  * **Curl:** Spines curl inward on themselves, with the direction determined by the force's rotation.  
  * **Planar:** Spines grow along a 2D plane defined by the force's rotation.  
  * **Mesh (Obstacles/Attractors):** Spines grow towards, away from, or wrap around an arbitrary mesh object. This is used for effects like roots growing over rocks, vines on a trellis, or plants avoiding obstacles. Mesh forces can obstruct growth or prune intersecting spines.  
  * **Return:** Causes branches to try and return to their original growth direction, useful for taming overly disturbed branches.  
  * **Season Light:** Influences how quickly leaves change season based on their orientation towards this force object.  
  * **Gravity:** A built-in force, often a property within Spine Generators, pulls branches downwards. Positive values pull down, negative values push up. This is equivalent to a global downward Direction force.  
* **Force Properties:**  
  * **Strength:** Controls the intensity of the force's influence.  
  * **Attenuation:** Limits the force's effect to a specific radius around the force object, allowing for localized influence.  
  * **Type-Specific Properties:** Each force type has unique parameters (e.g., curl axis, mesh action for collision).  
* **Wind as a Special Force:**  
  * Wind is a specialized dynamic force system in SpeedTree, crucial for animating vegetation.  
  * It's typically controlled via a "Fan" object in the scene and global/per-generator wind properties.  
  * The **Wind Wizard** can help automatically set up realistic wind parameters based on plant type and desired conditions.  
  * Wind effects include global motion, branch oscillation, and leaf rustling/tumbling. Parameters control distance, frequency, and direction adherence for different components.  
  * SpeedTree's wind system is designed to be scalable, from simple sways to complex cinematic effects, and is often implemented in shaders for real-time performance.

Forces are essential for breaking procedural uniformity and adding organic, environmentally-influenced shapes to vegetation models. The Mesh Force, in particular, is powerful for creating interactions between plants and their surroundings or even other parts of the same plant (using Collections).

#### **2\. Mesh Fronds and Leaf Meshes: Techniques for creating detailed foliage.**

SpeedTree provides two primary ways to incorporate custom mesh geometry for foliage: Mesh Fronds and Leaf Meshes. These are vital for creating detailed and varied leaves, flowers, fruits, and other complex plant parts.

* **Leaf Meshes:**  
  * **Function:** The **Leaf Mesh Generator** (or older "Leaf Generator") is used to place instances of 3D meshes as leaves. These meshes are typically created externally or using SpeedTree's Cutout Editor from a texture atlas.  
  * **Application:** Ideal for individual leaves, petals, small fruits, or any repeating mesh element that needs to be scattered across branches.  
  * **Control:** Properties control size, count, orientation (e.g., sky sensitivity, surface adhesion, roll), and material. Multiple mesh assets can be assigned to a single Leaf Mesh Generator, and SpeedTree will randomly pick from them based on weight settings, allowing for easy variation.  
  * **Optimization:** Leaf meshes can be simple cards (two triangles) or more complex 3D geometry. For real-time, optimized cards created with the Cutout Editor are common.  
  * **Techniques for Detail:**  
    * Use high-quality textures with detailed albedo, normal, and opacity maps.  
    * Employ multiple distinct leaf meshes within one generator for natural variation in shape and size.  
    * Utilize variance on size and orientation properties.  
    * For compound leaves (like palmate cannabis leaves), a common technique is to model the entire compound leaf (all leaflets and petiole) as a single mesh and place it with one Leaf Mesh generator. Alternatively, one could construct it procedurally by attaching individual leaflet meshes to a small, central "petiole" spine, though this is more complex.  
* **Mesh Fronds:**  
  * **Function:** A **Mesh Frond** uses a custom 3D mesh to replace the standard planar geometry typically generated by a Frond Generator. The custom mesh is deformed along the path of the underlying frond spine.  
  * **Application:** Excellent for elements that have a clear central spine or axis but require more complex 3D form than a simple plane. Examples include palm fronds, ferns, complex flowers, fruits growing along a stem (like grapes), pine cones, or even hand-carved branches used as detail elements.  
  * **Control:**  
    * The shape of the underlying procedural frond (controlled by properties like Frond:Shape:Width, Height, and profile curves) still influences the deformation of the custom mesh frond.  
    * Mesh Fronds typically expect the source mesh to be aligned along the Y-axis in its local space.  
    * Segmentation of the underlying spine (Accuracy, Length segments) is crucial for the mesh frond to deform smoothly and accurately capture the detail of the custom mesh.  
    * Frond shape segments also affect the mesh's cross-sectional deformation.  
  * **Limitations/Considerations:**  
    * Mesh Fronds generally do not respect a Count value greater than 1 per node (unlike standard fronds which can have multiple blades).  
    * Proper alignment (e.g., Frond:Roll) is important to match branch orientation.  
  * **Techniques for Detail:**  
    * Use Mesh Fronds to place pre-modeled flower clusters or fruit bunches along a stem.  
    * Create complex, two-sided leaves with thickness and vein detail that would be difficult with simple cards.  
    * Model unique bark formations or fungal growths as mesh fronds and apply them to trunks/branches.

Both Leaf Meshes and Mesh Fronds are managed via the **Meshes Bar**, where custom meshes are imported and their properties (like orientation and wind settings) can be adjusted. Choosing between them depends on whether the element is a discrete object placed at points (Leaf Mesh) or a continuous form deformed along a spine (Mesh Frond).

#### **3\. Decorations and Props: Adding non-procedural elements.**

SpeedTree allows for the integration of non-procedural or pre-modeled elements as "decorations" or "props" within the procedural environment. This is typically achieved by importing custom meshes and using specific generators or forces to place them.

* **Using Imported Meshes:**  
  * Custom meshes in formats like.FBX or.OBJ can be imported into the **Meshes Bar**. These meshes can represent anything from fruits, flowers, lanterns, signs, to small rocks or debris.  
* **Placement Mechanisms:**  
  * **Leaf Mesh Generator:** Can be used to scatter instances of these prop meshes onto branches or other surfaces, much like placing leaves. This is suitable for numerous small decorations.  
  * **Mesh Frond Generator:** If the prop has a somewhat elongated form or needs to be deformed along a path (e.g., a string of lights, a hanging ornament), a Mesh Frond could be used.  
  * **Decal Generator:** For projecting flat details or simple geometry onto surfaces (like a plaque or a patch of moss), the Decal Generator is suitable. It wraps a material and a flat mesh onto a parent.  
  * **Knot/Fin/Shell Generators:** These can create specialized geometric details that act as decorations, such as knots, fungal growths, or moss layers.  
  * **Mesh Force used as a Prop:** A Mesh Force can be used to simply place a static mesh in the scene without it necessarily affecting the growth of other SpeedTree elements. By assigning a material to the mesh used by the Mesh Force, it appears as a regular object. The "Include in Model" property on the Mesh Force ensures it exports with the tree. This is a common way to position larger, unique props relative to the tree.  
  * **Reference Generator:** Can be used to instance pre-configured prop assemblies within the tree structure.  
* **Control and Variation:**  
  * Placement, orientation, and scale of these props can be controlled procedurally using the generator's properties (frequency, distribution, variance).  
  * Node editing can be used to fine-tune the position of individual prop instances.  
* **Workflow:**  
  1. Model the prop/decoration in an external DCC application (e.g., Maya, Blender).  
  2. Export it as an.FBX or.OBJ.  
  3. Import the mesh into SpeedTree's Meshes Bar.  
  4. Create a material for the prop in the Materials Bar and assign textures.  
  5. Use an appropriate generator (Leaf Mesh, Mesh Frond, Decal) or a Mesh Force to place instances of the prop mesh within the SpeedTree scene.  
  6. Adjust generator properties for desired distribution and variation.

This capability allows artists to enrich their procedural vegetation with specific, art-directed elements, blending the strengths of procedural generation with custom asset integration.

#### **4\. Mutations and Randomization: Leveraging random seeds and variance controls for plant uniqueness.**

Achieving natural-looking variation and uniqueness in procedurally generated plants is a core strength of SpeedTree, primarily accomplished through its randomization system, driven by random seeds and variance controls.

* **Variance Controls:**  
  * As discussed previously (Section II.B.3), most numeric properties in SpeedTree generators have an associated **variance** value. This value defines a range (+/-) around the base property value from which a random value will be chosen for each individual node created by that generator.  
  * **Example:** A Branch:Spine:Length of 5.0 with a variance of 1.0 will produce branches with lengths randomly distributed between 4.0 and 6.0.  
  * **Application:** Applying variance to properties like length, radius, angle, count, and even curve points allows for subtle (or significant) differences between otherwise identical elements, mimicking natural growth irregularities.  
  * **Pro-Tip:** Small variances in key properties are often sufficient to create convincing variations without making the plant look chaotic or unnatural.  
* **Random Seeds:**  
  * Each generator in SpeedTree has a set of **random seeds** that influence all its randomized properties. Changing these seed values will produce a different set of random numbers, leading to a different iteration of the procedural generation for that generator and its children, even if all other properties and variances remain the same.  
  * **Random Seeds Property Group:** This group within a generator's properties allows users to view and modify seeds for different aspects (e.g., style, placement). A "Randomize all" button within this group will re-roll all seeds for that generator.  
  * **Global Randomization:** The **Generation Editor toolbar** has a "Randomize" button that can randomize selected generators or the entire model if nothing is selected. This effectively changes the seeds, producing a new "mutation" or variation of the plant.  
  * **Style (Legacy vs. Optimal):** SpeedTree 9.4 introduced an "Optimal" random seed generation style that better accounts for descendants, recommended for new models. A "Legacy" option is available for compatibility with older models.  
* **Leveraging for Plant Uniqueness ("Mutations"):**  
  * **Creating Base Strains:** Define a set of core parameters and variance ranges for a specific plant "strain" or archetype.  
  * **Generating Instances:** By simply changing the main random seed of the Tree Generator (or key sub-generators), numerous unique visual instances (phenotypes) of that strain can be generated quickly. Each instance will share the core characteristics but differ in the exact placement, length, and angle of its branches, leaves, etc.  
  * **Controlled Mutations:** For more targeted mutations, instead of randomizing everything, an artist might adjust specific seed values within a generator or slightly tweak variance amounts for particular properties.  
  * **Rules System:** The "Rules" system can also be used to create controls that drive randomization or select specific seed sets, potentially linking these to external genetic data for "Project Chimera".  
* **Best Practices for Randomization :**  
  * **Use Liberally Early, Sparingly Late:** Randomize frequently during the initial stages of modeling to ensure the procedural setup is robust and variance ranges are appropriate.  
  * **Be Cautious with Manual Edits:** Randomizing a generator (or its ancestors) can cause node edits or hand-drawn elements to be lost or significantly altered, as the underlying structure may change. It's often best to finalize procedural generation and randomization before making extensive manual tweaks.  
  * **Randomize Individual Generators:** Remember that randomization can be applied to specific generators, not just the entire tree, for more focused changes.

By skillfully combining base property values, variance ranges, and the manipulation of random seeds, artists can efficiently produce a vast array of unique plant individuals from a single procedural definition, crucial for populating natural-looking environments.

#### **5\. Growth Animations (if applicable to subtle plant movements or state changes, not full lifecycle yet).**

SpeedTree includes a "Growth" animation system designed to simulate the gradual development of a plant over time, or more subtle movements and state changes, rather than necessarily a complete seed-to-maturity lifecycle in one go.

* **Core Components for Growth:**  
  * **Timeline Bar:** This is where growth is enabled, and its overall timing and speed are controlled. It features a "Speed" control and a curve to adjust how fast the model grows over the defined frame range.  
  * **"Growth" Property Groups:** Specific generators (Branch, Leaf Mesh, Fin, Frond, Cap, Knot) have a "Growth" group in their properties. These properties control aspects like timing relative to the parent, growth speed scalars, and orientation changes during growth.  
  * **Growth Wizard:** Accessible from the "Tools" menu or the Timeline Bar, the Growth Wizard helps set up basic growth parameters automatically. It can configure the model to "Grow" (organic development) or "Reveal" (traced out, good for vines on trellises).  
* **Workflow for Growth Animation :**  
  1. **Model Correction:** Finalize the plant model with growth disabled.  
  2. **Run Growth Wizard:** Use the wizard to establish initial growth settings. This often involves converting Batched Leaf generators to Leaf Mesh generators, as the former don't support growth.  
  3. **Enable Growth on Timeline:** Activate the "Growth" feature on the Timeline Bar.  
  4. **Adjust Timing & Speed:** Use the Timeline's "Speed" control and "End" frame to set the overall duration and pace.  
  5. **Fine-Tune Generator Properties:** Modify the "Growth" properties on individual generators to control how different parts of the plant develop (e.g., making leaves appear after branches have extended).  
  6. **Preview:** Scrub the timeline to preview the animation. The "Focus" feature can isolate parts of the tree for faster previews on complex models. Image sequence export can also be used for previewing.  
* **Applications for Subtle Movements or State Changes:**  
  * While not its primary design for full cannabis lifecycle (which involves drastic morphological shifts), the growth system could be adapted for:  
    * **Subtle Unfurling/Opening:** Animating leaves unfurling or flowers slowly opening over a short period.  
    * **Positional Shifts:** Simulating slight changes in branch orientation due to phototropism or wilting over time, if these can be tied to growth parameters.  
    * **Appearance of New Elements:** Making new leaves or small branches appear gradually.  
  * The "Speed" of growth can be very slow, and specific "Growth" properties on generators (like Start Scalars:Orientation) might be animated to create these subtle effects.  
* **Limitations for Full Lifecycle :**  
  * The system is designed for gradual development. Radical structural changes (e.g., from a seedling to a mature, multi-branched plant) are better handled by separate models for each major stage.  
  * Certain SpeedTree features can cause artifacts during growth animation (e.g., bifurcation, some alignment properties, jaggedness). The Growth Wizard attempts to mitigate these by adjusting relevant properties (e.g., setting Spine:Welding:Style to "Conform").  
* **Exporting Growth Animations:**  
  * Growth animations, due to changing vertex counts per frame, must be exported as Alembic (.abc) files.

For "Project Chimera," while distinct models will likely represent major lifecycle stages, the Growth animation system could be valuable for visualizing subtle transitions *within* a stage or for specific animated details like flower blooming or leaf unfurling, if required.

#### **6\. Seasonal variations (and how these might be adapted for representing different plant health states or genetic expressions).**

SpeedTree includes a "Seasons" system primarily designed to simulate the typical seasonal changes of deciduous trees (e.g., leaves changing color in autumn and dropping). However, this system's underlying mechanics of blending between material states can be creatively adapted to represent other state changes, such as variations in plant health or genetic expression.

* **Core Mechanics of the Season System:**  
  * **Timeline Bar / Season Slider:** A global "Season" value (ranging from 0.0 to 1.0) is controlled either by a slider on the Viewport toolbar (older versions) or via a curve on the Timeline Bar (newer versions). This global value drives the seasonal transition.  
  * **Generator Season Properties:** Leaf and Branch generators have "Season" property groups that determine how individual nodes react to the global season value. These properties compute a "transition value" for each node, indicating its stage in the seasonal cycle. Key properties include Start offset (when transition begins) and Time scale (duration of transition).  
  * **Material Season Curves & Texture Blending:**  
    * Materials (especially leaf materials) have "Season Curves." The "transition value" of a node is used to look up this curve, determining the probability of that material being selected or, more commonly, how textures are blended.  
    * A common setup for deciduous trees involves placing the Spring/Summer leaf texture in the material's main diffuse slot and the Fall/Autumn texture in the detail map slot. The Season system then blends between these two textures based on the node's transition state. The Change duration property's profile curve can make leaf edges turn color before the center.  
  * **Material Sets:** Can be used to organize materials for different seasons, simplifying management.  
* **Adapting for Plant Health or Genetic Expression:** The core idea is to repurpose the "season" concept to represent different plant states. Instead of "Spring," "Summer," "Autumn," one might define states like "Healthy," "Nutrient Deficient," "Diseased," or different visual expressions of a gene.  
  * **Health States:**  
    * **Healthy State (e.g., Season \= 0.0):** Materials use vibrant green textures, upright posture parameters.  
    * **Nutrient Deficient State (e.g., Season \= 0.5):**  
      * **Material Blending:** The "detail map" slot in leaf materials could hold textures representing chlorosis (yellowing) or necrosis (browning). The Season system would blend towards these textures as the "health" slider (repurposed Season slider) moves towards the "deficient" state.  
      * **Parameter Changes:** While the Season system primarily affects materials, for structural changes like wilting, other parameters would need to be linked. This might involve using the "Rules" system or Pipeline SDK to tie the "health" state to branch angle properties (making them droop) or leaf curl parameters.  
    * **Diseased State (e.g., Season \= 1.0):** Similar to nutrient deficiency, but with textures showing lesions, spots, or more severe discoloration.  
  * **Genetic Expressions:**  
    * If a gene influences, for example, leaf coloration (e.g., purple hues in some cannabis strains), the Season system could blend between a standard green leaf material and a purple-tinged leaf material. The "Season" slider would then represent the expression level of this gene.  
    * For more complex structural differences due to genetics (e.g., Indica vs. Sativa branching), the Season system is less suitable; these would typically be handled by different base generator setups or significant parameter changes via Rules/SDK.  
  * **Implementation:**  
    1. Define the visual characteristics of each state (healthy, unhealthy, specific genetic trait).  
    2. Create appropriate PBR texture sets for each state (e.g., healthy leaf albedo, yellowed leaf albedo).  
    3. In SpeedTree, assign the "base" state texture (e.g., healthy) to the diffuse slot and the "alternative" state texture (e.g., unhealthy) to the detail slot of relevant materials.  
    4. Configure the Season properties on generators and the Season Curves on materials to control the transition logic.  
    5. The external GxE system for "Project Chimera" would then drive the global "Season" value in SpeedTree (interpreted as "Health" or "GeneExpression") to reflect the simulated state of the plant.

This adaptation allows for dynamic visual changes based on simulated conditions without needing entirely separate models for each minor health variation or genetic nuance, leveraging SpeedTree's existing material blending capabilities. For major structural changes, however, direct parameter manipulation via Rules/SDK or separate models would still be necessary.

#### **7\. Photogrammetry Workflows: Integrating scanned assets or textures.**

SpeedTree 10 offers robust workflows for integrating photogrammetry data, allowing artists to combine the realism of scanned assets with the flexibility of procedural modeling. This is particularly useful for creating highly realistic trunks, branches, or unique details.

* **Core Capabilities:**  
  * **Mesh Converter / Photogrammetry Conversion:** SpeedTree includes tools (often part of the Mesh Converter Generator or related photogrammetry features) to process scanned meshes. This can involve:  
    * Capturing textures from the scan (baking albedo, normal, displacement maps).  
    * Converting parts of the scan (e.g., a trunk) into a procedural SpeedTree model, allowing further procedural growth or modification.  
    * Extending scanned geometry with new procedural SpeedTree geometry.  
  * **Mesh Spines System:** As mentioned (Section II.B.5), this SpeedTree 10 feature is crucial for photogrammetry. It allows artists to rig an imported scanned mesh with procedural spines. This enables:  
    * Growing new procedural branches or leaves directly off the surface of the scan.  
    * Applying SpeedTree's wind animation to the scanned mesh by deforming these embedded spines.  
    * Exporting the rigged scan with animation data.  
  * **Using Scans as References or Components:**  
    * Import scanned meshes as static references in the scene.  
    * Use scanned textures (bark, leaves) on procedural SpeedTree geometry.  
    * Incorporate smaller scanned elements (e.g., unique knots, small branches, fruit) as Mesh Fronds or Leaf Meshes within a larger procedural structure.  
* **General Workflow for Integrating Scanned Trunks/Branches :**  
  1. **Acquire and Process Scan:** Obtain a 3D scan of the plant part (e.g., using photogrammetry software like 3DF Zephyr ). Clean up the mesh and generate initial textures (albedo, normal).  
  2. **Import into SpeedTree:** Import the scanned mesh (.FBX,.OBJ) into the Meshes Bar.  
  3. **Texture Creation/Refinement:**  
     * Use the **Mesh Converter Generator** to create tiling map sets from the scanned trunk textures. This helps in seamlessly blending procedural extensions.  
  4. **Procedural Extension (if needed):**  
     * **Stitch Generator:** Joins native SpeedTree geometry (e.g., a new procedural top for a scanned trunk) to the imported mesh. Different stitch types exist (bake stitch, band stitch, wrap stitch) for various blending results.  
     * **Mesh Spines:** Rig the scanned trunk with Mesh Spines using the Mesh Helper tool. Then, new procedural Branch generators can be attached to these spines to grow branches directly from the scan's surface.  
  5. **Placing Nodes on Scanned Meshes:**  
     * The **Target Generator** can be used to precisely locate and orient children (e.g., branches, leaves) on the surface of any mesh, including imported scans.  
     * Hand-drawing tools can also be used to place branches originating from the scanned surface.  
  6. **Material Setup:** Apply the baked textures from the scan (and any procedurally generated textures) to the appropriate parts of the model using PBR materials.  
* **Using Scanned Leaf/Twig Atlases:**  
  1. Scan or photograph leaf/twig clusters.  
  2. Process these into texture atlases (albedo, normal, opacity, etc.).  
  3. In SpeedTree, create materials using these atlases.  
  4. Use the Cutout Editor to create efficient meshes (cards) for these leaf/twig clusters.  
  5. Place these meshes using Leaf Mesh generators.

The integration of photogrammetry allows for a significant boost in realism by incorporating real-world details, while SpeedTree's procedural tools provide the means to customize, extend, and animate these scanned assets. The Mesh Spines system in SpeedTree 10 is a particularly powerful enhancement for this workflow.

#### **8\. Collection and Library Management: Creating and using collections of SpeedTree assets.**

SpeedTree offers several ways to manage and reuse assets, both within a single project and across multiple projects. This includes saving generator templates, entire models, and using the "Collections" feature for dynamic in-scene geometry reuse.

* **Saving and Loading.SPM Files (SpeedTree Procedural Models):**  
  * The native file format for SpeedTree models is.SPM. These files store all generator hierarchies, properties, curve data, material assignments, and node edits.  
  * Saving.SPM files allows for entire plant models to be reused or versioned. Options include "Save," "Save As," "Save Incremental," and "Save As with Assets" (which gathers all associated textures and meshes into a specified location).  
* **Generator Templates (.STT Files):**  
  * Any generator or a hierarchy of generators (e.g., a complex branch setup with leaves) can be saved as a template (.STT file).  
  * **Creation:** Right-click a generator in the Generation Editor and select "Save Template" (or similar wording depending on version).  
  * **Usage:** Right-click on a parent generator and choose "Add template to selected..." to load a saved.STT file, instantly adding that pre-configured structure to the current model.  
  * **Benefits:** Massively speeds up workflow by allowing artists to build a library of reusable plant components (e.g., standard trunk types, specific branching patterns, leaf clusters).  
* **SpeedTree Library (Official Asset Store):**  
  * SpeedTree offers an official asset library where users can purchase professionally made, production-ready plant models.  
  * These assets typically come with PBR materials, seasonal variations, and wind animations, serving as excellent starting points or ready-to-use elements.  
  * Access to these libraries can be an optional add-on to licenses like Indie or Pro.  
* **"Collections" Feature:**  
  * **Function:** Collections are a powerful in-scene feature that allows geometry generated by one set of generators to be dynamically captured as a mesh asset *during the tree model computation*. This captured mesh can then be used by other generators or forces within the *same*.SPM file as if it were a standard imported mesh.  
  * **Creation:**  
    1. Add a new blank mesh asset in the Meshes Bar.  
    2. Check the "Collection" box for this mesh asset.  
    3. In the "Generation" properties of the generator(s) whose output you want to capture (e.g., a Trunk generator), find the "Collections" group and check the box corresponding to the newly created collection asset.  
    4. The "Create Collection From Selection" tool (Tools menu) can automate this setup.  
  * **Usage:**  
    * The collection (which is now a dynamic mesh asset) can be assigned to a Zone Generator to grow other elements (like ivy) directly on the surface of the collected geometry.  
    * It can be used with a Mesh Force to make other branches collide with or wrap around the collected geometry (e.g., self-colliding roots, vines growing over a trunk).  
  * **Key Considerations:**  
    * **Order Matters:** Generators contributing to a collection must compute *before* generators that use that collection. In the Generation Editor, this means contributing generators should generally be on lower levels or to the left of using generators.  
    * **Dynamic Updates:** If the geometry contributing to the collection changes, the collection mesh updates automatically, and any dependent elements will re-adapt.  
  * **Collections vs. Templates:**  
    * **Templates (.STT):** Reusable building blocks (generator setups) that can be imported into *any*.SPM file. They are static definitions.  
    * **Collections:** An in-scene mechanism for one part of a *single*.SPM model to dynamically provide its geometry as a mesh source for another part of the *same* model. They are for intra-model interaction and geometry reuse.

Effective library management through saving.SPM files and.STT templates, combined with the dynamic capabilities of Collections for complex intra-model interactions, provides a flexible and efficient framework for asset creation and reuse in SpeedTree.

#### **9\. Scripting (if available/applicable in SpeedTree 10): Extending functionality.**

SpeedTree 10 offers scripting capabilities primarily through its "Rules" system and, for more extensive external control, the "Pipeline SDK." These allow users to extend functionality, automate tasks, and integrate SpeedTree with other systems.

* **SpeedTree Rules System:**  
  * **Function:** Rules are Lua scripts embedded within a SpeedTree model (.SPM file) that allow users to design a customized editing interface (in the "Rules" window) with controls like sliders, checkboxes, and dropdowns. These custom controls can then manipulate one or multiple existing generator properties, often in a combined or abstracted way.  
  * **Capabilities :**  
    * **Simplify Complex Adjustments:** Combine multiple low-level property changes into a single high-level slider (e.g., an "Age" slider that affects trunk length, radius, branch density, and leaf size simultaneously).  
    * **Range Limitation:** Constrain property manipulation within specific artist-defined ranges.  
    * **Scale Normalization:** Control properties using normalized scales (e.g., 0-1) for intuitive proportional adjustments.  
    * **Curve Interpolation:** Automatically adjust property curves by interpolating between predefined states.  
  * **Workflow :**  
    1. Define a Rule using Lua script (e.g., rule\_int("MyRule", 0, 0, 10\) creates an integer slider named "MyRule" from 0 to 10).  
    2. Process the value from this Rule control within the script if necessary.  
    3. Use set\_property\_value(), set\_property\_profile(), etc., functions to assign the processed value to target generator properties.  
  * **Use Cases for "Project Chimera":**  
    * Creating artist-friendly controls for common cannabis adjustments (e.g., "Strain Vigor," "Leaf Droop Factor").  
    * Exposing parameters that can be easily understood and manipulated by an AI agent for high-level task generation within the Modeler.  
    * Simulating GxE traits by linking a Rule (e.g., "HeightPotential") to multiple underlying generator parameters.  
* **SpeedTree Pipeline SDK:**  
  * **Function:** An Enterprise-exclusive C++ Software Development Kit (with Python and C\# wrappers) that allows for headless (no GUI) generation and modification of procedural tree models *outside* of the SpeedTree Modeler application.  
  * **Capabilities :**  
    * Parse.SPM files.  
    * Modify generator properties programmatically.  
    * Utilize embedded Rules within the.SPM file.  
    * Adjust polygonal resolution, seasonal attributes, and random seeds.  
    * Export models to various formats (.FBX,.OBJ, Alembic, etc.) and their textures.  
  * **Use Cases for "Project Chimera":**  
    * **Direct GxE Integration:** The primary mechanism for "Project Chimera's" GxE simulation engine to programmatically alter plant parameters without manual Modeler intervention. The GxE system could load a base cannabis.SPM, modify its parameters based on simulated genetic and environmental data, and then export the resulting unique phenotype for use in Unreal Engine.  
    * **Batch Generation:** Automate the creation of large libraries of plant variations based on different GxE inputs.  
    * **AI-Driven Asset Creation:** An AI agent could use the Pipeline SDK to generate specific plant models based on descriptive prompts by manipulating.SPM parameters.  
* **Export Scripting:**  
  * SpeedTree 10 also allows for scripting custom export presets, enabling studios to tailor the export process to their specific pipeline needs.

The Rules system is more for enhancing the interactive workflow within the Modeler, providing artists (or AI-assisted artists) with simplified controls. The Pipeline SDK, on the other hand, is for deep, programmatic integration and automation, making it the more direct tool for systems like "Project Chimera's" GxE simulation to dynamically drive plant generation and variation. Both scripting avenues are powerful for extending SpeedTree's core functionality.

#### **10\. Understanding and troubleshooting common modeling issues.**

While SpeedTree is a robust tool, users may occasionally encounter modeling issues or unexpected behavior. Understanding common problems and knowing where to find solutions is key to a smooth workflow.

* **Common Issues (derived from bug fix lists and general 3D practices):**  
  * **Hand-Drawing/Node Edits:**  
    * Deleted hand-drawn nodes not restoring correctly upon undo (fixed in some versions).  
    * Node edits being lost or behaving unexpectedly after randomizing parent generators. This is often by design, as randomization can fundamentally change the underlying structure.  
    * **Pro-Tip:** Finalize procedural generation and randomization before extensive node editing or hand-drawing. Save incrementally.  
  * **LODs and Performance:**  
    * LOD "fuzziness" or incorrect restoration when loading models.  
    * Crashes related to editing LOD curves if not set up correctly.  
    * Visible "popping" during LOD transitions if changes between levels are too abrupt (e.g., excessive segment reduction).  
    * **Pro-Tip:** Test LOD transitions thoroughly. Ensure smooth visual changes.  
  * **Mesh and UV Issues:**  
    * Incorrect UV coordinates (e.g., swapped U and V on some platforms for.OBJ export).  
    * Bad texture coordinates resulting from high "Jaggedness" values on branches.  
    * Problems with mesh replacement or normals on deleted nodes.  
    * Lightmapping issues in game engines due to improper UV layout (overlapping UVs, insufficient padding) are a general 3D concern applicable here.  
    * **Pro-Tip:** Pay close attention to UVs, especially for lightmapping. Use SpeedTree's unwrapping and atlasing tools carefully for game exports.  
  * **Export/Import Problems:**  
    * Errors in exported files (e.g., Alembic on Linux, incorrect node parent info in XML).  
    * Path problems with scripts for DCCs like Maya or Houdini.  
    * **Pro-Tip:** Always use the latest recommended export presets for your target engine/DCC. Ensure scripts and plugins are up to date.  
  * **Material/Rendering Artifacts:**  
    * Leaf ripple "pulsing" in exports.  
    * Incorrect background or light colors in the viewport on some OpenGL platforms.  
    * **Pro-Tip:** Use PBR validation render modes (Albedo Check, Subsurface Check) to ensure material values are within plausible ranges.  
  * **Application Stability:**  
    * Crashes during specific operations (e.g., subdivision generator deletion, leaf "Keep" curve misconfiguration, force mesh creation via certain methods).  
    * Hangs during lightmap computation (though fixes are often implemented).  
    * **Pro-Tip:** Save work frequently. If encountering persistent crashes, try to isolate the problematic generator or operation by simplifying the model.  
* **Troubleshooting Resources:**  
  * **Official Documentation:** The SpeedTree documentation (often hosted by Unity for recent versions) includes release notes detailing bug fixes, which can indicate past common issues. The documentation map can lead to specific feature explanations.  
  * **SpeedTree Forums:** A valuable resource for getting help from developers and other users, reporting bugs, and sharing solutions.  
  * **Official Tutorials & YouTube Channel:** Can provide insights into correct workflows, potentially highlighting common mistakes.  
  * **Support Contact:** For licensed users, direct support from SpeedTree/IDV/Unity is available.  
  * **General 3D Community:** Broader 3D art forums (e.g., Polycount, ArtStation) may have users discussing SpeedTree workflows and solutions to general 3D problems that apply to SpeedTree assets (e.g., texturing, optimization).  
* **General Troubleshooting Strategies:**  
  * **Isolate the Problem:** If a model behaves unexpectedly, disable generators one by one to identify which one is causing the issue.  
  * **Check Simple Things First:** Ensure materials are correctly assigned, textures are loaded, and basic generator parameters are sensible.  
  * **Reset Properties/Generators:** If a generator is acting erratically, try resetting its properties to default or re-creating it.  
  * **Consult Release Notes:** If an issue appears after a software update, check the release notes for known issues or changes in behavior.

By being aware of potential pitfalls and knowing where to seek help, users can navigate the complexities of advanced 3D modeling in SpeedTree more effectively.

## **III. Specialized Application: Generating Cannabis Plant Assets for "Project Chimera"**

This section focuses on the specific application of SpeedTree 10 for creating diverse and realistic cannabis plant assets, as required by "Project Chimera." This involves a detailed understanding of cannabis morphology and translating it into effective SpeedTree techniques, including procedural variation based on genetic and environmental (GxE) data, and representing the plant's full lifecycle.

### **A. Deconstructing Cannabis Morphology for SpeedTree**

A thorough understanding of cannabis plant anatomy is paramount for its accurate procedural generation. The morphology varies significantly based on genetics (strain), environmental conditions, and cultivation techniques.

#### **1\. Detailed analysis of typical and atypical cannabis plant structures:**

* **Main Stalk (Stem):** The central stem of the cannabis plant is typically angular, furrowed, and can possess a woody interior, which may be hollow between the nodes (internodes). It provides primary support for all lateral branches. Cultivation techniques like "topping" (removing the apical bud) can induce the main stalk to split, leading to multiple main stems and, consequently, multiple dominant colas. Some larger Sativa strains may develop longer, more lignified (wood-like) stems.  
* **Branching Patterns:** Branches emerge from nodes, which are the intersections on the stem where leaves, bracts, and pre-flowers also develop.  
  * **Internodal Spacing:** The distance between nodes varies considerably. Indica-dominant strains typically exhibit short internodal spacing, resulting in a dense, bushy structure. Conversely, Sativa-dominant strains tend to have larger internodal spacing, leading to taller, more open, and "stretchy" plants. Environmental factors like wide temperature fluctuations or inadequate light can also induce stretching and increased internodal length.  
  * **Branching Angles and Density:** These are also strain-dependent. Indicas are generally more densely branched. Training techniques significantly alter branching:  
    * **Topping:** Cutting the main stem above a node encourages the two sub-apical branches to become dominant, increasing the number of main colas and promoting a bushier structure.  
    * **Low-Stress Training (LST):** Involves gently bending and tying down branches to create a more even canopy, improve light penetration to lower branches, and control plant height and shape.  
* **Leaf Morphology:** Cannabis leaves are characteristically palmate, meaning they are compound with multiple leaflets radiating from a common point, much like the fingers of a hand.  
  * **Number of Leaflets:** Typically, a mature cannabis leaf has 5-7 serrated leaflets (lobes). However, this number can vary significantly depending on the plant's age, position on the plant, and genetic factors. Seedlings start with single leaflets (first true leaves), then progress to three, five, seven, and even nine leaflets on mature leaves from middle nodes. Leaf complexity can then decrease again towards the top of the plant or on flowering shoots, sometimes reverting to single leaflets.  
  * **Serration:** Leaflet margins are distinctly serrated (saw-toothed).  
  * **Size Variation:** Leaf size varies along the plant, with the largest fan leaves typically found on the main stem and larger branches in the vegetative stage. Leaf area generally peaks around the 12th node. Leaflets themselves are unequal in size within a single palmate leaf.  
  * **Arrangement (Phyllotaxy):** Leaf arrangement transitions during growth. On the lower part of the plant (vegetative stage), leaves are often opposite (two leaves per node on opposing sides). Higher up the plant, particularly as it approaches and enters the flowering stage, the arrangement typically becomes alternate (one leaf per node).  
  * **Types of Leaves:**  
    * **Fan Leaves:** These are the large, iconic leaves responsible for the majority of photosynthesis. They contain very low concentrations of THC.  
    * **Sugar Leaves:** These are smaller leaves found intermingled with and protruding from the bud clusters (colas). They are named for the dense coating of crystalline trichomes they often develop, making them rich in cannabinoids and terpenes.  
* **Bud/Cola Formation:** The "bud" or "cola" is the flowering part of the female cannabis plant, highly valued for its cannabinoid content.  
  * **Structure and Density:** A cola is a cluster of tightly packed individual female flowers (pistillate flowers). The main cola, often called the apical bud, forms at the top of the main stem or dominant branches. Smaller colas can develop at bud sites along lower branches. Density varies by strain and growing conditions.  
  * **Calyx:** Each individual flower unit is largely composed of a calyx (plural: calyces), a tear-drop shaped structure that encloses the plant's reproductive parts \[

#### **Works cited**

1\. What's the difference between SpeedTree Games and Cinema licenses? \- Unity Support, https://support.unity.com/hc/en-us/articles/30093221172628-What-s-the-difference-between-SpeedTree-Games-and-Cinema-licenses 2\. How do I activate my SpeedTree license? \- Unity Support, https://support.unity.com/hc/en-us/articles/15723462234772-How-do-I-activate-my-SpeedTree-license 3\. SpeedTree: Learning Edition, https://store.speedtree.com/store/speedtree-learning-edition-v10/ 4\. What's the difference between SpeedTree Learning Edition, Indie, Pro, and Enterprise?, https://support.unity.com/hc/en-us/articles/15723241438228-What-s-the-difference-between-SpeedTree-Learning-Edition-Indie-Pro-and-Enterprise 5\. SpeedTree: Indie, https://store.speedtree.com/store/speedtree-indie-2/ 6\. SpeedTree Software INFO, https://store.speedtree.com/games/modeler10/ 7\. SpeedTree: Pro License – SpeedTree, https://store.speedtree.com/store/speedtree-pro-license/ 8\. SpeedTree: Pro, https://store.speedtree.com/store/speedtree-pro/ 9\. SpeedTree Cinema, https://store.speedtree.com/cinema/ 10\. SpeedTree 10, https://store.speedtree.com/speedtree10/ 11\. Art Tools – SpeedTree, https://store.speedtree.com/category/art-tools/feed/ 12\. Frequently Asked Questions (F.A.Q.) \- SpeedTree, https://store.speedtree.com/faq/ 13\. SpeedTree Modeler 10 User Manual, https://docs.unity3d.com/speedtree-modeler/manual/ 14\. How do I activate my SpeedTree Floating license? \- Unity Support, https://support.unity.com/hc/en-us/articles/34430822379412-How-do-I-activate-my-SpeedTree-Floating-license 15\. welcome \[SpeedTree Documentation\], https://docs.speedtree.com/doku.php?id=welcome 16\. Documentation map \- Unity \- Manual, https://docs.unity3d.com/speedtree-modeler/manual/doc-map.html 17\. SpeedTree 10 | Official Tutorials \- YouTube, https://www.youtube.com/playlist?list=PLXu-oi6XTKP1XGqy0SU-vw0gg3QW06kk- 18\. SpeedTree Tutorial: Modeler Basics \- YouTube, https://www.youtube.com/watch?v=sbmQhdQ\_2VI\&pp=0gcJCdgAo7VqN5tD 19\. SpeedTree 10: Introduction to the new Interface \- YouTube, https://www.youtube.com/watch?v=YT3Cm-bN-cg 20\. start \[SpeedTree Documentation\], https://docs9.speedtree.com/modeler/ 21\. Nodes \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=nodes 22\. generators \[SpeedTree Documentation\], https://docs8.speedtree.com/modeler/doku.php?id=generators 23\. Generators \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=generators 24\. Generation Editor \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=generation\_editortw 25\. Hand Drawing \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=handdrawing 26\. Property Editor \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=propertyeditoroverview 27\. Curve Editor \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=toolcurve\_editor 28\. curves\_overview \[SpeedTree Documentation\], https://docs8.speedtree.com/modeler/doku.php?id=curves\_overview 29\. Modeling with Curves \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=modelingwithcurves 30\. Materials and maps \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=kcmaterialspbr 31\. meshes \[SpeedTree Documentation\], https://docs8.speedtree.com/modeler/doku.php?id=meshes 32\. Modeling \> Custom Mesh Assets \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=generate\_mesh 33\. seasons \[SpeedTree Documentation\], https://docs8.speedtree.com/modeler/doku.php?id=seasons 34\. Create a mesh cutout \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=create-a-mesh-cutout 35\. Collision Primitives \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=collision 36\. overview \[SpeedTree Documentation\], https://docs.speedtree.com/doku.php?id=overview 37\. Collections \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=collections 38\. Season \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=season 39\. Growth \- Unity \- Manual, https://docs.unity3d.com/speedtree-modeler/manual/growth.html 40\. Introduction to Rules, https://docs.unity3d.com/speedtree-modeler/manual/rules-introduction.html 41\. SpeedTree functions usable in Rules \- Unity \- Manual, https://docs.unity3d.com/speedtree-modeler/manual/rules-speedtree-functions.html 42\. Modeling Approach \- SpeedTree 8, https://docs8.speedtree.com/modeler/doku.php?id=kcmodelingapproach 43\. docs.unity3d.com, https://docs.unity3d.com/speedtree-modeler/manual/materials-and-maps.html 44\. Getting Started \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=gettingstarted 45\. Modeling Walkthrough \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=modeling\_walkthrough 46\. Hand drawing \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=kchanddrawing 47\. Hand Drawing \- SpeedTree 8, https://docs8.speedtree.com/modeler/doku.php?id=kchanddrawing 48\. SpeedTree tips and tricks – master the basics of this industry-standard software, https://gradientgroup.com/speedtree-tips-and-tricks-master-the-basics-of-this-industry-standard-software/ 49\. Global Tree Properties \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=global\_tree\_properties 50\. Spine Generator \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=spine\_generator 51\. Leaf Mesh Generator \- SpeedTree 8, https://docs8.speedtree.com/modeler/doku.php?id=leaf\_mesh\_generator 52\. Leaf generator \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=leaf\_generator 53\. Frond generator \- Unity \- Manual, https://docs.unity3d.com/speedtree-modeler/manual/frond-generator.html 54\. mesh\_fronds \[SpeedTree Documentation\], https://docs.speedtree.com/doku.php?id=mesh\_fronds 55\. Zone Generator \- SpeedTree 8, https://docs8.speedtree.com/modeler/doku.php?id=zone\_generator 56\. Zone generator \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=zone\_generator 57\. Knot Generator \- SpeedTree 8, https://docs8.speedtree.com/modeler/doku.php?id=knot\_generator 58\. What can we do with SpeedTree 10 in Unity? | Unity Render Farm \- iRender, https://irendering.net/what-can-we-do-with-speedtree-10-in-unity/ 59\. SpeedTree 10: Vines \- YouTube, https://www.youtube.com/watch?v=MFUq7\_uwSZ8 60\. SpeedTree Tutorial: Mesh Detail And Decal Generators \- ArtStation, https://www.artstation.com/artwork/mA0b9a 61\. Decal generator properties \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=decal\_generator 62\. Photogrammetry in SpeedTree \- Unity \- Manual, https://docs.unity3d.com/speedtree-modeler/manual/photogrammetry-in-speedtree.html 63\. SpeedTree | The Industry Standard for Procedural Modeling \- Unity, https://unity.com/products/speedtree 64\. Documentation \- SpeedTree, https://store.speedtree.com/support/documentation/ 65\. Speed Tree for Beginners Course Free Chapters \- YouTube, https://www.youtube.com/watch?v=NMaOBpVqJiM 66\. Nodes \- Unity \- Manual, https://docs.unity3d.com/speedtree-modeler/manual/nodes.html 67\. Curves \- Unity \- Manual, https://docs.unity3d.com/speedtree-modeler/manual/curves.html 68\. kcrandomizex \[SpeedTree Documentation\], https://docs9.speedtree.com/modeler/doku.php?id=kcrandomizex 69\. Branch Generator \- SpeedTree 8, https://docs8.speedtree.com/modeler/doku.php?id=branch\_generator 70\. Branch generator \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=branch\_generator 71\. SpeedTree 10: Using your Hero Mesh \- YouTube, https://www.youtube.com/watch?v=VtLaEIThUAo 72\. SpeedTree Tutorial: Using Custom Images and Meshes in the Modeler \- YouTube, https://www.youtube.com/watch?v=O-fSEYQzJHA 73\. Add spines to a hero mesh \- Unity \- Manual, https://docs.unity3d.com/speedtree-modeler/manual/mesh-helpers-add-spines.html 74\. SpeedTree \- Training Series \- 001 \- InterFace \- YouTube, https://www.youtube.com/watch?v=pd4iUID7wDI 75\. Speedtree 10 Zero to Hero Course \- YouTube, https://www.youtube.com/watch?v=LpcTo\_\_naqA 76\. Free SpeedTree Tutorial: Build a basic tree from scratch \- Pluralsight \- YouTube, https://www.youtube.com/watch?v=2Gn9CAdkL8o 77\. Speedtree Tutorial \- How to Create a Oak Tree \- YouTube, https://www.youtube.com/watch?v=3VHlOtbH5Q8 78\. Easy Foliage for Games using SpeedTree & Unreal Engine | FastTrackTutorials \- Skillshare, https://www.skillshare.com/en/classes/easy-foliage-for-games-using-speedtree-and-unreal-engine/1561568200 79\. Create, open, or save files \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=create\_open\_or\_save\_files 80\. Materials Bar \- SpeedTree 8, https://docs8.speedtree.com/modeler/doku.php?id=materials\_assets\_bar 81\. Phyllotaxy \- SpeedTree 8, https://docs8.speedtree.com/modeler/doku.php?id=genmode\_phyllotaxy 82\. Photogrammetry for Vegetation \- Part 9 \- CREATING LEAF CLUSTERS AND FINAL TREE IN SPEEDTREE \- YouTube, https://www.youtube.com/watch?v=ixpbmsJ-i-w 83\. SpeedTree Cinema 8: Leaf Mesh Cutout Tool \- YouTube, https://www.youtube.com/watch?v=r0RB6cpcGdk 84\. Must Know Techniques for Making Stylized Plants using SpeedTree \- YouTube, https://www.youtube.com/watch?v=hfbbzjK\_6lo 85\. SpeedTree Tutorial: Modeling with Forces \- YouTube, https://www.youtube.com/watch?v=sm3qhShUBlQ 86\. Forces \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=forces 87\. SpeedTree \- Training Series \- 003 \- Forces \- YouTube, https://www.youtube.com/watch?v=xEo-51cJgLw 88\. SpeedTree \- Hand Drawing Tutorial (Unreal Engine 4\) \- YouTube, https://www.youtube.com/watch?v=Ex05qvoviHU 89\. resolution \[SpeedTree Documentation\], https://docs8.speedtree.com/modeler/doku.php?id=resolution 90\. lod \[SpeedTree Documentation\], https://docs8.speedtree.com/modeler/doku.php?id=lod 91\. Importing into Unreal Engine (Legacy) \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=impue4 92\. impue4st9 \[SpeedTree Documentation\] \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=impue4st9 93\. Exporting from SpeedTree Indie \- YouTube, https://www.youtube.com/watch?v=5qlSc6HmjIw 94\. Games export options \- Unity \- Manual, https://docs.unity3d.com/speedtree-modeler/manual/games-export-options.html 95\. Unwrap UVs \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=expunwrap 96\. User Interface \> Exporting Unwrapped Meshes \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=exporting\_unwrapped 97\. exporting \[SpeedTree Documentation\], https://docs.speedtree.com/doku.php?id=exporting 98\. houdini\_speedtree\_material \[SpeedTree Documentation\], https://docs.speedtree.com/doku.php?id=houdini\_speedtree\_material 99\. SpeedTree Cinema 8: Intro to PBR \- YouTube, https://www.youtube.com/watch?v=YYnI2mJhjZg 100\. PBR Materials \- SpeedTree 8, https://docs8.speedtree.com/modeler/doku.php?id=materials\_pbr 101\. Materials \- Unity \- Manual, https://docs.unity3d.com/speedtree-pipeline-sdk/manual/materials.html 102\. Design – SpeedTree, https://store.speedtree.com/tag/design/feed/ 103\. SpeedTree Tutorial: Mesh Forces \- YouTube, https://www.youtube.com/watch?v=nJ7oKEI9M2s 104\. Make animated windy tree from scratch in SpeedTree and export it to your software, https://www.youtube.com/watch?v=K\_wyY5euTyw 105\. High Detail Modeling \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=high\_detail\_modeling\_tips 106\. Wind Overview \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=wind\_overview 107\. Wind Wizard \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=windwizard 108\. Creating a Mysterious Scene with Lifelike Foliage Using SpeedTree \- 80 Level, https://80.lv/articles/creating-a-mysterious-scene-with-lifelike-foliage-using-speedtree 109\. How to use SpeedTree to create foliage for Unreal Engine PART 2 ( TUTORIAL ) \- YouTube, https://www.youtube.com/watch?v=KnPZtSbqX4U 110\. Foxtail Palm | Speedtree Cinema 9 Tutorial \- YouTube, https://www.youtube.com/watch?v=wRY3cr56pWI 111\. SpeedTree Tutorial: Mesh detail and decal generators \- YouTube, https://www.youtube.com/watch?v=x\_39t21C9y8 112\. Random seeds properties \- SpeedTree 9 Documentation, https://docs9.speedtree.com/modeler/doku.php?id=random-seeds-properties 113\. Learn how to generate an animated pedestrian scene using ActorCore, iClone & Omniverse, https://magazine.reallusion.com/post\_tag/timecode/page/43/ 114\. SpeedTree \- Tutorial \- Creating Basil Plant Growth Animation \- YouTube, https://www.youtube.com/watch?v=-up85fXXreE 115\. Animated Growth \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=growth 116\. Seasons \- Unity \- Manual, https://docs.unity3d.com/speedtree-modeler/manual/seasons.html 117\. SpeedTree Pro \- 6-month Subscription (floating), https://www.motionmedia.com/speedtree-pro-6-month-subscription-floating/ 118\. SpeedTree \- Unity Manual, https://docs.unity.cn/es/2019.4/Manual/SpeedTree.html 119\. Collections \- SpeedTree 8, https://docs8.speedtree.com/modeler/doku.php?id=collections 120\. SDK Organization \- SpeedTree Documentation, https://docs.speedtree.com/doku.php?id=sdk\_organization 121\. qsue4 \[SpeedTree Documentation\], https://docs8.speedtree.com/modeler/doku.php?id=qsue4 122\. SpeedTree – 3D Vegetation Modeling and Middleware, https://store.speedtree.com/ 123\. Runtime SDK \- SpeedTree, https://store.speedtree.com/runtime-sdk/ 124\. SpeedTree Pipeline SDK User Manual, https://docs.unity3d.com/speedtree-pipeline-sdk/ 125\. Python Bindings for Pipeline SDK \- Unity \- Manual, https://docs.unity3d.com/speedtree-pipeline-sdk/manual/binding-python.html 126\. Pipeline SDK features and capabilities \- Unity \- Manual, https://docs.unity3d.com/speedtree-pipeline-sdk/manual/features-capabilities.html 127\. Support \- SpeedTree, https://store.speedtree.com/support/ 128\. what\_s\_new \[SpeedTree Documentation\], https://docs.speedtree.com/doku.php?id=what\_s\_new 129\. The 11 Best 3D Modeling Software Packages | Rendernode, https://www.rendernode.com/3d-modeling-software/ 130\. Troubleshooting \- Unity \- Manual, https://docs.unity3d.com/6000.1/Documentation/Manual/upm-errors.html 131\. KB\_Collated articles \- 01 – Lumion \- User Support, https://support.lumion.com/hc/en-us/articles/18419204198556-KB-Collated-articles-01 132\. SpeedTree \- Unity \- Manual, https://docs.unity3d.com/2019.3/Documentation/Manual/SpeedTree.html 133\. Understanding Lightmapping in Unreal Engine \- Epic Games Developers, https://dev.epicgames.com/documentation/en-us/unreal-engine/understanding-lightmapping-in-unreal-engine 134\. Cannabis Plant Morphology \- Emerald Harvest, https://emeraldharvest.co/cannabis-plant-morphology/ 135\. Cannabis Plant Anatomy \- Solaris Farms, https://solarisfarms.org/cannabis-plant-anatomy/ 136\. Inside the Cannabis Cola: Expert Deep Dive \- The Triminator, https://thetriminator.com/cannabis-anatomy-101-understanding-the-cannabis-cola/ 137\. Topping and Low Stress Training (LST) \- Seed Supreme Help Center, https://help.seedsupreme.com/en-US/training---topping-and-low-stress-training-(lst)-710826 138\. Cannabis Plant Anatomy: Nodes And Internodes \- RQS Blog \- Royal Queen Seeds, https://www.royalqueenseeds.com/us/blog-cannabis-plant-anatomy-nodes-and-internodes-n559 139\. Morphological Characterization of Cannabis sativa L. Throughout Its Complete Life Cycle, https://pubmed.ncbi.nlm.nih.gov/37896109/ 140\. Understanding the Weed Leaf: Your Guide to Cannabis Anatomy | Blog \- Verts Dispensary, https://vertsdispensary.com/understanding-the-weed-leaf-your-guide-to-cannabis-anatomy/ 141\. Weed Leaf Guide: Decoding Fan vs Sugar Leaves \- Essential Tips for Growers \- Vivosun, https://vivosun.com/growing\_guide/different-types-of-weed-leaf/ 142\. Plant Anatomy \- Dakota Natural Growers, https://dakotanaturalgrowers.com/plant-anatomy 143\. Cannabis Plant Anatomy: Essential Parts That Make up a Cannabis Plant \- MMI Agriculture Solutions, https://mmiagriculture.com/cannabis-plant-anatomy/