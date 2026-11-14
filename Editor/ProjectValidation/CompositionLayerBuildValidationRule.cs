using System;
using Unity.XR.CoreUtils.Editor;

namespace UnityEditor.XR.CompositionLayers.Editor.ProjectValidation
{
    /// <summary>
    /// Project validation rule class for composition layer package.
    /// </summary>
    [System.Obsolete("CompositionLayerBuildValidationRule is not intended for use outside the Composition Layers package. It is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release.")]
    public class CompositionLayerBuildValidationRule : BuildValidationRule
    {

        /// <summary>
        /// Used to Determine which Build Target to display the Validation Rules for.
        /// Unknown is used to display the rules for all Build Targets.
        /// </summary>
        internal BuildTargetGroup buildTargetGroup = BuildTargetGroup.Unknown;

        /// <summary>
        /// Whether the rule is hidden while the composition layer manager is not running.
        /// </summary>
        internal bool requiresActiveManager = true;
    }
}
