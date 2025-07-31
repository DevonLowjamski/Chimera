**5.2 Core Entities & Relationships (Conceptual ERD)**  
This section describes the primary entities (represented as tables) and their relationships, forming the conceptual blueprint of the softwares database. Foreign keys (FK) indicate relationships between tables.  
**5.2.1 Farm & Location Hierarchy:** Defines the physical layout and structure of the cultivation facility.  
`Farms` (Table): Represents a single cultivation operation/facility.  
`farm_id` (PK)  
`farm_name`  
`address_details` (Potentially JSONB or separate related table)  
`geographic_coordinates` (PostGIS `POINT` type)    
`created_at`, `updated_at`  
`LocationTypes` (Table): Defines templates for location hierarchies (e.g., "Veg Room", "Flower Room", "Greenhouse Bay").  
`location_type_id` (PK)  
`type_name` (e.g., "Veg Room", "Flower Greenhouse")  
`hierarchy_definition` (JSONB defining levels like \["House", "Row", "Table"\])    
`created_at`, `updated_at`  
 `Areas` (Table): Represents specific, trackable growing or functional locations within the farm. This is a central entity linked to many others.  
`area_id` (PK, potentially BIGINT for scalability)    
`farm_id` (FK \-\> Farms)  
`area_label` (User-friendly name/alias)    
`location_type_id` (FK \-\> LocationTypes)    
`location_path` (TEXT or other type storing hierarchical position, e.g., "Veg House 1/Row 2/Table 4-6")    
`spatial_geometry` (PostGIS `POLYGON` or `MULTIPOLYGON` type for map representation)    
`area_attributes` (JSONB for additional characteristics like size, lighting type, etc.)    
`current_stage` (TEXT, e.g., "Veg 1", "Flower 3", "Empty")    
`weeks_in_veg` (INTEGER, nullable)    
`weeks_in_flower` (INTEGER, nullable)    
`stage_start_date` (DATE, nullable)  
`barcode_id` (TEXT, Unique, Nullable \- linked to Barcodes table)    
`created_at`, `updated_at`  
 `AreaStageHistory` (Table): Tracks changes in the primary stage of an Area.  
`stage_history_id` (PK)  
`area_id` (FK \-\> Areas)  
`stage_name` (TEXT)  
`start_date` (TIMESTAMP WITH TIME ZONE)  
`end_date` (TIMESTAMP WITH TIME ZONE, nullable for current stage)  
`trigger` (TEXT, "Automated" or "Manual")    
`user_id` (FK \-\> Users, nullable for automated changes)  
`notes` (TEXT, nullable)  
 

**5.2.2 Plant & Batch Tracking:** Manages information about plant groups or optionally individual plants.  
`Strains` (Table): Catalog of strains/genetics used.  
`strain_id` (PK)  
`strain_name` (Unique)  
`breeder` (TEXT, nullable)  
`genetic_lineage` (TEXT, nullable)  
`phenotype_description` (TEXT, nullable)  
`expected_flowering_time_days` (INTEGER, nullable)  
`typical_profile` (JSONB for cannabinoid/terpene info)    
`created_at`, `updated_at`  
`PlantBatches` (Table): Represents a group of plants managed together (primary tracking unit).  
`batch_id` (PK, potentially BIGINT)  
`batch_label` (User-friendly alias, nullable)  
`area_id` (FK \-\> Areas \- Current location)  
`strain_id` (FK \-\> Strains)    
`plant_type` (TEXT, "Clone" or "Seed")    
`source_batch_id` (FK \-\> PlantBatches, for tracking clone source/mother plant batch, nullable)  
`planting_date` (DATE)    
`initial_plant_count` (INTEGER)  
`current_plant_count` (INTEGER, can be updated via events)    
`batch_status` (TEXT, e.g., "Active", "Harvested", "Destroyed")  
`created_at`, `updated_at`  
`IndividualPlants` (Table): Optional table for detailed tracking if needed.  
`plant_id` (PK, potentially BIGINT)    
`plant_alias` (User-friendly alias, nullable)    
`batch_id` (FK \-\> PlantBatches)  
`plant_tag_id` (TEXT, unique, nullable \- e.g., physical RFID or tag number)  
`initial_location_description` (TEXT, e.g., "Table 3, Position 5", nullable)  
`planting_date` (DATE)    
`plant_type` (TEXT, "Clone" or "Seed")    
`is_mother_plant` (BOOLEAN, default FALSE)  
`plant_status` (TEXT, e.g., "Active", "Harvested", "Culled")  
`created_at`, `updated_at`  
 `PlantBatchMovementHistory` (Table): Tracks movement of batches between areas.  
`movement_id` (PK)  
`batch_id` (FK \-\> PlantBatches)  
`from_area_id` (FK \-\> Areas, nullable for initial placement)  
`to_area_id` (FK \-\> Areas)  
`movement_date` (TIMESTAMP WITH TIME ZONE)  
`user_id` (FK \-\> Users)  
`notes` (TEXT, nullable)

**5.2.3 Environmental Data:** Stores sensor configurations and readings.  
`Sensors` (Table): Configuration details for each deployed sensor.  
`sensor_id` (PK)  
`sensor_label` (User-defined name)    
`sensor_type` (TEXT, e.g., "Temperature", "Humidity", "pH", "EC", "Light", "CO2")    
`area_id` (FK \-\> Areas \- Location assignment)    
`connection_method` (TEXT, e.g., "API", "Modbus", "Manual")    
`connection_details` (JSONB for API endpoint, keys, Modbus address, etc.)    
`measurement_units` (TEXT, e.g., "Celsius", "%RH", "pH", "µS/cm", "PPFD")    
`reading_frequency_seconds` (INTEGER, nullable for manual)  
`last_reading_time` (TIMESTAMP WITH TIME ZONE, nullable)  
`connection_status` (TEXT, e.g., "Connected", "Disconnected", "Error")    
`is_enabled` (BOOLEAN, default TRUE)  
`created_at`, `updated_at`  
`SensorReadings` (Table): Stores individual time-series readings. (Consider partitioning by `reading_time` and potentially using TimescaleDB extension )  
`reading_time` (TIMESTAMP WITH TIME ZONE, part of PK/Hypertable index)  
`sensor_id` (FK \-\> Sensors, part of PK/Hypertable index)  
`area_id` (FK \-\> Areas \- denormalized for query performance, can be derived from `sensor_id`)    
`parameter_type` (TEXT \- denormalized, can be derived)    
`reading_value` (NUMERIC or DOUBLE PRECISION)    
`reading_units` (TEXT \- denormalized)    
`data_quality_flags` (INTEGER or TEXT, nullable, for marking outliers, errors)    
`EnvironmentalAggregates` (Table(s)): Stores pre-calculated aggregates (e.g., hourly, daily averages, min/max). Schema would vary based on aggregation interval (e.g., `HourlyEnvironmentalAggregates`, `DailyEnvironmentalAggregates`).  
`aggregate_time` (TIMESTAMP WITH TIME ZONE, PK)  
`area_id` (FK \-\> Areas, PK)  
`parameter_type` (TEXT, PK)  
`avg_value` (NUMERIC)  
`min_value` (NUMERIC)  
`max_value` (NUMERIC)  
`std_dev_value` (NUMERIC, nullable)  
`reading_count` (INTEGER)  
 

**5.2.4 Event & Log Data:** Captures discrete events and observations.  
`AreaEventLog` (Table): Central log for discrete events linked to Areas. (Consider partitioning by `event_time`)  
`event_log_id` (PK, BIGSERIAL)  
`area_id` (FK \-\> Areas)  
`batch_id` (FK \-\> PlantBatches, nullable if event not batch specific)  
`event_time` (TIMESTAMP WITH TIME ZONE)  
`event_type` (TEXT, e.g., "Irrigation", "IPM Treatment", "Manual Reading", "Observation", "Stage Change", "Data Import")    
`user_id` (FK \-\> Users, nullable for system events)  
`event_data` (JSONB for flexible, type-specific details)  
Example Irrigation: `{"duration_minutes": 30, "volume_liters": 150, "nutrient_solution_id": 12, "notes": "Flushing cycle"}`  
Example IPM: `{"product_used": "Neem Oil", "target": "Spider Mites", "method": "Spray", "applicator_user_id": 5}`  
Example Manual Reading: `{"measurement_type": "pH", "value": 6.2, "method": "In-pot Probe", "meter_id": "11-01"}`  
Example Observation: `{"category": "PlantHealth", "notes": "Slight yellowing on lower leaves observed."}`  
 `source` (TEXT, e.g., "Manual Entry", "Spreadsheet Import", "Sensor System", "System Automation")    
`import_log_id` (FK \-\> ImportLogs, nullable)  
 

**5.2.5 Task Management Data:** Stores task definitions, assignments, and history.  
`TaskCategories` (Table): User-definable categories for tasks.  
`category_id` (PK)  
`farm_id` (FK \-\> Farms)  
`category_name` (TEXT)  
`created_at`, `updated_at`  
 `Tasks` (Table): Defines individual tasks.  
`task_id` (PK)  
`farm_id` (FK \-\> Farms)  
`task_name` (TEXT)    
`description` (TEXT, nullable)    
`category_id` (FK \-\> TaskCategories, nullable)    
`area_id` (FK \-\> Areas, nullable for global tasks)    
`assigned_user_id` (FK \-\> Users, nullable)    
`assigned_team_id` (FK \-\> Teams, nullable)    
`priority` (TEXT, e.g., "High", "Medium", "Low")    
`due_date` (TIMESTAMP WITH TIME ZONE, nullable)    
`start_date` (TIMESTAMP WITH TIME ZONE, nullable)    
`is_recurring` (BOOLEAN, default FALSE)    
`recurrence_rule` (JSONB or TEXT storing recurrence pattern, nullable)  
`current_status` (TEXT, e.g., "Open", "In Progress", "Completed")    
`created_by_user_id` (FK \-\> Users)    
`created_at`, `updated_at`  
`TaskStatusHistory` (Table): Logs changes in task status.  
`status_history_id` (PK)  
`task_id` (FK \-\> Tasks)  
`status` (TEXT)  
`change_time` (TIMESTAMP WITH TIME ZONE)  
`user_id` (FK \-\> Users, nullable for system changes)  
`notes` (TEXT, nullable)  
`TaskNotes` (Table): Stores notes/comments added to tasks.  
`note_id` (PK)  
`task_id` (FK \-\> Tasks)  
`user_id` (FK \-\> Users)  
`note_text` (TEXT)  
`created_at`  
 `TaskAssignments` (Table): Manages assignments if tasks can be assigned to multiple users/teams or claimed from a pool. (Alternative to storing directly in `Tasks` table if more complex assignment logic is needed).  
`assignment_id` (PK)  
`task_id` (FK \-\> Tasks)  
`user_id` (FK \-\> Users, nullable)  
`team_id` (FK \-\> Teams, nullable)  
`assignment_time` (TIMESTAMP WITH TIME ZONE)  
`assignment_status` (TEXT, e.g., "Assigned", "Claimed", "Unassigned")  
 

**5.2.6 Knowledge Base Data:** Stores uploaded documents and their metadata.  
`KnowledgeItems` (Table): Represents an ingested document, image, or web resource.  
`item_id` (PK)  
`farm_id` (FK \-\> Farms)  
`item_type` (TEXT, e.g., "PDF", "DOCX", "Image", "Web URL")    
`original_filename` (TEXT, nullable)  
`source_url` (TEXT, nullable)  
`storage_path` (TEXT, path to stored file in cloud storage/local disk)    
`title` (TEXT, potentially extracted or user-defined)  
`extracted_text` (TEXT, potentially stored in a related large object table if very large)    
`processing_status` (TEXT, e.g., "Uploaded", "Processing", "Processed", "Error")    
`uploaded_by_user_id` (FK \-\> Users)  
`created_at`, `updated_at`  
`KnowledgeCategories` (Table): Predefined categories for organizing knowledge items.  
`category_id` (PK)  
`category_name` (TEXT, unique)  
`description` (TEXT, nullable)  
 `KnowledgeTags` (Table): Controlled vocabulary tags.  
`tag_id` (PK)  
`tag_name` (TEXT, unique)  
 `KnowledgeItemCategories` (Table): Many-to-many relationship between items and categories.  
`item_id` (FK \-\> KnowledgeItems, PK)  
`category_id` (FK \-\> KnowledgeCategories, PK)  
`confidence_score` (NUMERIC, nullable \- for automated suggestions)    
`is_user_confirmed` (BOOLEAN, default FALSE)  
`KnowledgeItemTags` (Table): Many-to-many relationship between items and tags.  
`item_tag_id` (PK)  
`item_id` (FK \-\> KnowledgeItems)  
`tag_id` (FK \-\> KnowledgeTags, nullable \- for open tags)  
`open_tag_name` (TEXT, nullable \- for user-defined tags)    
`confidence_score` (NUMERIC, nullable)    
`is_user_confirmed` (BOOLEAN, default FALSE)

**5.2.7 User & Access Control Data:** Manages users, roles, permissions, and teams.  
`Users` (Table): Stores user account information.  
`user_id` (PK)  
`farm_id` (FK \-\> Farms)  
`username` (TEXT, unique)  
`email` (TEXT, unique)  
`password_hash` (TEXT \- securely hashed)    
`first_name` (TEXT, nullable)  
`last_name` (TEXT, nullable)  
`mfa_secret` (TEXT, nullable \- encrypted)  
`is_active` (BOOLEAN, default TRUE)  
`created_at`, `updated_at`  
`Roles` (Table): Defines system roles.  
`role_id` (PK)  
`role_name` (TEXT, unique, e.g., "Admin", "Grower", "Analyst")  
`description` (TEXT, nullable)  
 `Permissions` (Table): Defines granular permissions.  
`permission_id` (PK)  
`permission_name` (TEXT, unique, e.g., "view\_tasks", "edit\_area", "delete\_user")  
`description` (TEXT, nullable)  
`RolePermissions` (Table): Many-to-many relationship linking roles to permissions.  
`role_id` (FK \-\> Roles, PK)  
`permission_id` (FK \-\> Permissions, PK)  
`UserRoles` (Table): Many-to-many relationship linking users to roles.  
`user_id` (FK \-\> Users, PK)  
`role_id` (FK \-\> Roles, PK)  
`Teams` (Table): Defines user teams for assignment.  
`team_id` (PK)  
`farm_id` (FK \-\> Farms)  
`team_name` (TEXT)  
 `TeamMembers` (Table): Links users to teams.  
`team_id` (FK \-\> Teams, PK)  
`user_id` (FK \-\> Users, PK)

**5.2.8 Configuration & Settings Data:** Stores system and user-level settings.  
`SystemSettings` (Table): Global application settings.  
`setting_key` (TEXT, PK)  
`setting_value` (JSONB or TEXT)  
`description` (TEXT, nullable)  
`UserSettings` (Table): User-specific preferences.  
`user_id` (FK \-\> Users, PK)  
`setting_key` (TEXT, PK)  
`setting_value` (JSONB or TEXT)  
`AlertRules` (Table): Configuration for the Alert & Notification system.  
 `DataValidationRules` (Table): Configuration for data quality rules.  
`rule_id` (PK)  
`rule_name` (TEXT)  
`description` (TEXT)  
`target_data_category` (TEXT)  
`target_field` (TEXT)  
`rule_definition` (JSONB or TEXT for rule logic)  
`is_enabled` (BOOLEAN)

**5.2.9 AI & Simulation Data:** Stores metadata about AI models and simulation scenarios/results.  
`AIModels` (Table): Registry of deployed AI models.  
`model_id` (PK)  
`model_name` (TEXT)  
`model_type` (TEXT, e.g., "Yield Prediction", "Disease Classification")  
`current_version` (TEXT)  
`description` (TEXT)  
`deployment_status` (TEXT)  
 `AIModelVersions` (Table): Tracks versions of models.  
`version_id` (PK)  
`model_id` (FK \-\> AIModels)  
`version_number` (TEXT)  
`training_dataset_ref` (TEXT or FK to a Datasets table)  
`performance_metrics` (JSONB)  
`deployment_date` (TIMESTAMP WITH TIME ZONE)  
 `SimulationScenarios` (Table): Stores definitions of user-created "what-if" scenarios.  
`scenario_id` (PK)  
`scenario_name` (TEXT)  
`user_id` (FK \-\> Users)  
`base_scenario_id` (FK \-\> SimulationScenarios, nullable)  
`scenario_parameters` (JSONB defining input adjustments)  
`created_at`  
 `SimulationResults` (Table): Stores the output results of simulations.  
`result_id` (PK)  
`scenario_id` (FK \-\> SimulationScenarios)  
`model_version_id` (FK \-\> AIModelVersions)  
`simulation_time` (TIMESTAMP WITH TIME ZONE)  
`predicted_outputs` (JSONB for storing time-series predictions, distributions, etc.)    
`comparison_metrics` (JSONB, if comparing scenarios)  
