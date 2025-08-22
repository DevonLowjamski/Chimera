# Project Chimera

An ambitious multi-genre cannabis cultivation simulation game blending detailed infrastructure management, genetic mastery, creative construction, and strategic optimization.

## Project Structure

### Assets/ProjectChimera/
- **Core/**: Core systems, managers, and utilities
- **Data/**: ScriptableObject definitions organized by domain
  - **Genetics/**: Gene definitions, strain data, trait libraries
  - **Environment/**: Environmental parameters and recipes
  - **Equipment/**: Equipment specifications and configurations
  - **Economy/**: Market data, contracts, NPC profiles
  - **Progression/**: Skill trees and research projects
  - **Events/**: Event channels for decoupled communication
- **Systems/**: Game system implementations
  - **Genetics/**: Inheritance, breeding logic, trait expression
  - **Cultivation/**: Plant lifecycle, GxE simulation
  - **Environment/**: HVAC, microclimate modeling
  - **Construction/**: Building systems, utility networks
  - **DataCollection/**: Sensors, analytics, logging
  - **Economy/**: Contracts, marketplace, resources
  - **Progression/**: Skills, research, narrative
  - **Automation/**: Controllers and scheduling
- **UI/**: User interface systems
- **Visuals/**: 3D models, materials, and effects
- **Testing/**: Comprehensive testing framework

## Assembly Structure

The project uses a modular assembly definition structure:
- `ProjectChimera.Core`: Foundation systems and utilities
- `ProjectChimera.Data`: ScriptableObject definitions
- `ProjectChimera.Genetics`: Genetics and breeding systems
- `ProjectChimera.Cultivation`: Plant lifecycle and growth
- `ProjectChimera.Environment`: Environmental simulation
- `ProjectChimera.Construction`: Building and facilities
- `ProjectChimera.Economy`: Economic systems
- `ProjectChimera.Progression`: Skills and research
- `ProjectChimera.UI`: User interface
- `ProjectChimera.Testing`: Testing utilities

## Development Philosophy

- **ScriptableObject-driven**: Extensive use of SO for data configuration
- **Event-driven architecture**: Decoupled systems using SO-based events
- **Modular design**: Clear separation of concerns with well-defined APIs
- **SOLID principles**: Maintainable, testable code architecture
- **Comprehensive testing**: Automated validation and performance monitoring

## Getting Started

1. Open the project in Unity 6000.2.0b2 (Unity 6 Beta)
2. Ensure Universal Render Pipeline (URP) is configured
3. Run initial data validation tests
4. Review architecture documentation in Documentation/

## Key Features

- **Deep Genetics Simulation**: Sophisticated inheritance and breeding mechanics
- **GxE Modeling**: Genotype × Environment interaction simulation  
- **Facility Management**: Grid-based construction with utility networks
- **Data-Driven Decision Making**: Comprehensive analytics and visualization
- **Player-Controlled Time**: Flexible time scaling with offline progression
- **Earned Automation**: Progressive automation unlocks through skill development

## Documentation

- **Architecture/**: System architecture and design patterns
- **API/**: Code documentation and API references
- **Design/**: Game design specifications and balance documents
- **Testing/**: Testing protocols and validation procedures

---

For detailed information, see the comprehensive design documentation in the `.cursor/rules/Chimera md/` directory.