using System.Xml;
using Verse;

namespace ImmersiveChefs;

/// <summary>
/// Removes matching XML nodes when one exact package ID is active. RimWorld's
/// built-in mod-conditional patch operation matches mutable display names.
/// </summary>
public sealed class PatchOperationRemoveIfModActive : PatchOperationPathed
{
    private string packageId = string.Empty;

    protected override bool ApplyWorker(XmlDocument xml)
    {
        var packageActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
        if (!packageActive)
        {
            return true;
        }

        var matched = false;
        foreach (XmlNode node in xml.SelectNodes(xpath)!)
        {
            if (node.ParentNode is null)
            {
                continue;
            }

            node.ParentNode.RemoveChild(node);
            matched = true;
        }

        return matched;
    }

    public override string ToString()
    {
        return $"{base.ToString()}({packageId})";
    }
}
