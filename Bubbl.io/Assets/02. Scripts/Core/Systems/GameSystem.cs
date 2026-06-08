using _02._Scripts.Entity.Player;
using UnityEngine;

namespace _02._Scripts.Core.Systems
{
    public class GameSystem : MonoBehaviour, ISystem
    {
        [field:SerializeField] public Player Player { get; private set; }
        [field:SerializeField] public EnemyAgent Enemy { get; private set; }
        
        public void Initialize(System system) { }

        public void GameEnd(User loser)
        {
            if (loser == User.Player)
            {
                Player?.End(false);
                Enemy?.End(true);
            }
            else
            {
                Player?.End(true);
                Enemy?.End(false);
            }
        }
        
        public void Reset() { }
    }

    public enum User
    {
        Player,
        Enemy
    }
}