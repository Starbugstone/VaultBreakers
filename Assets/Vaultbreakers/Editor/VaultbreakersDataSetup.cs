using UnityEditor;
using UnityEngine;
using Vaultbreakers.Core;

namespace Vaultbreakers.Editor
{
    /// <summary>
    /// Creates the balance data the POC tunes through. Unlike the rest of the generated assets this
    /// one is deliberately not overwritten on a rebuild: the values in it are playtest results, and
    /// regenerating the foundation must never silently throw them away.
    /// </summary>
    internal static class VaultbreakersDataSetup
    {
        public static PrototypeBalance EnsureBalanceAsset()
        {
            var balance = AssetDatabase.LoadAssetAtPath<PrototypeBalance>(VaultbreakersSetupPaths.BalancePath);
            if (balance != null)
            {
                return balance;
            }

            balance = ScriptableObject.CreateInstance<PrototypeBalance>();
            balance.name = "PrototypeBalance";
            AssetDatabase.CreateAsset(balance, VaultbreakersSetupPaths.BalancePath);
            EditorUtility.SetDirty(balance);
            return balance;
        }
    }
}
