# Strategic File Size Guidelines & Refactoring Plan

## 📋 **File Type Size Guidelines**

### **🎯 Core Managers (100-200 lines)**
*Coordinate between systems, should be lean and focused*
- **Current Violations:**
  - AnalyticsManager.cs (1,175) ⚠️ **CRITICAL**
  - TimeManager.cs (1,066) ⚠️ **CRITICAL** 
  - CurrencyManager.cs (1,022) ⚠️ **CRITICAL**
  - CultivationManager.cs (938) ⚠️ **HIGH**

### **🔧 Services & Controllers (150-300 lines)**
*Handle specific business logic, should be focused*
- **Current Violations:**
  - SaveStorage.cs (1,131) ⚠️ **CRITICAL**
  - PlacementPaymentService.cs (977) ⚠️ **HIGH**
  - DomainSpecificOfflineHandlers.cs (969) ⚠️ **HIGH**

### **🖥️ UI Components (200-400 lines)**
*Handle specific UI responsibilities*
- **Current Violations:**
  - InputSystemIntegration.cs (990) ⚠️ **HIGH**
  - SettingsPanel.cs (951) ⚠️ **MEDIUM**
  - SaveLoadPanel.cs (936) ⚠️ **MEDIUM**

### **📊 Data Structures & DTOs (400-800 lines)**
*Data containers can be larger but should be domain-focused*
- **Assessment Needed:**
  - ContractDataStructures.cs (1,270) ⚠️ **REVIEW** (may need domain split)
  - FinanceDataStructures.cs (1,145) ⚠️ **REVIEW** (may need domain split)
  - EconomyDTO.cs (1,153) ⚠️ **MEDIUM** (probably OK if focused)

### **⚙️ ScriptableObjects (300-600 lines)**
*Configuration containers, can be data-heavy*
- **Current Files:**
  - FertigationSystemSO.cs (995) ✅ **ACCEPTABLE** (configuration data)

### **🧪 Test Files (500+ lines OK)**
*Can contain many test cases*
- **Current Files:**
  - AdvancedMenuSystemTest.cs (1,166) ✅ **OK**
  - DIGameManagerValidationTest.cs (962) ✅ **OK**

### **📚 Libraries & Utilities (200-500 lines)**
*Collections of related functionality*
- **Current Files:**
  - EffectsPrefabLibrary.cs (1,000) ⚠️ **MEDIUM** (could be split by effect type)

---

## 🚨 **Priority Refactoring Order**

### **Phase 1: Critical Managers (Days 1-3)**
1. **AnalyticsManager.cs** (1,175 → ~150 each)
   - Split: CoreAnalytics, EventAnalytics, PerformanceAnalytics, ReportingAnalytics
2. **TimeManager.cs** (1,066 → ~200 each)  
   - Split: TimeScale, OfflineProgression, TimeEvents, SaveTime
3. **CurrencyManager.cs** (1,022 → ~250 each)
   - Split: CurrencyCore, Transactions, EconomyBalance, ExchangeRates

### **Phase 2: Critical Services (Days 4-5)**
4. **SaveStorage.cs** (1,131 → ~300 each)
   - Split: SaveCore, LoadCore, SerializationHelpers, MigrationService
5. **PlacementPaymentService.cs** (977 → ~250 each)
   - Split: PlacementValidator, PaymentProcessor, CostCalculator, RefundHandler

### **Phase 3: High Priority (Days 6-7)**
6. **CultivationManager.cs** (938 → ~200 each)
   - Split: PlantLifecycle, EnvironmentControl, PlantCare, HarvestManager
7. **DomainSpecificOfflineHandlers.cs** (969 → ~200 each)
   - Split by domain: CultivationOffline, ConstructionOffline, EconomyOffline, GeneticsOffline

### **Phase 4: Medium Priority (Days 8-9)**
8. **UI Components** (3 files)
   - Split by functional areas and extract common UI logic
9. **Data Structure Review**
   - Analyze if ContractDataStructures needs domain splitting

---

## 🎯 **Strategic Principles**

### **Don't Refactor If:**
- ✅ File is mostly data/configuration (DTOs, ScriptableObjects)
- ✅ File is a test with many test cases
- ✅ File has high cohesion (all code relates to single responsibility)
- ✅ Breaking it would create artificial coupling

### **Do Refactor If:**
- ⚠️ File coordinates multiple systems (managers doing too much)
- ⚠️ File has multiple unrelated responsibilities 
- ⚠️ File would be hard for new developer to understand
- ⚠️ File has mixed abstraction levels
- ⚠️ File violates Single Responsibility Principle

### **Refactoring Patterns:**
1. **Extract by Responsibility** (most common)
2. **Extract by Data Domain** (for large DTOs)
3. **Extract by Lifecycle** (for managers handling multiple phases)
4. **Extract by Integration** (for services handling multiple external systems)

---

## 📊 **Success Metrics**

| File Type | Current Avg | Target Avg | Max Acceptable |
|-----------|------------|------------|----------------|
| Core Managers | 1,041 lines | 150 lines | 200 lines |
| Services | 1,026 lines | 225 lines | 300 lines |
| UI Components | 959 lines | 300 lines | 400 lines |
| Data/DTOs | 1,189 lines | 600 lines | 800 lines |

**Total Files to Refactor: 7 critical, 3 high priority = 10 files**
**Estimated Time: 9-10 days** (vs 74 files at 20+ days with arbitrary limits)

---

## 🔄 **Implementation Strategy**

1. **Start with highest impact** (Core Managers first)
2. **Maintain existing interfaces** during refactoring  
3. **Create integration tests** before splitting
4. **Use composition over inheritance** in new structure
5. **Extract interfaces** for all new components
6. **Update ServiceContainer** registrations for new components

This strategic approach focuses on **architectural impact** rather than arbitrary line counts, ensuring we fix the actual problems while avoiding unnecessary work.
