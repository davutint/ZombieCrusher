/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Company   :   Renowned Games
   Developer :   Tamerlan Shakirov
   ----------------------------------------------------------------
   Copyright 2024 Renowned Games All rights reserved.
   ================================================================ */

namespace RenownedGames.AITree
{
    public interface ISubTree
    {
        /// <summary>
        /// Associated instance of sub behaviour tree.
        /// </summary>
        BehaviourTree GetSubTree();
    }
}
