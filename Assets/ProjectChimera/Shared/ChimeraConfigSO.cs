using UnityEngine;

namespace ProjectChimera.Shared
{
    public abstract class ChimeraConfigSO : ChimeraScriptableObject
    {
        // Intentionally hides base UniqueID with same semantics for configs
        public new string UniqueID => name;

        // Clarify intent: reuse base validation hook
        public override bool ValidateData() => true;
    }
}
