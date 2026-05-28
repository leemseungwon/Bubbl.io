using _02._Scripts.Enum;

namespace _02._Scripts.Core.Managers
{
    public interface IManager
    {
        public void Initialize(Manager manager);
        public void LoadScene(SceneType sceneType);
        public void Reset();
    }
}