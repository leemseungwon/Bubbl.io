
using _02._Scripts.Ball;
using UnityEngine;

namespace _02._Scripts.Environment
{
    [RequireComponent(typeof(BoxCollider))]
    public class GameEndArea : MonoBehaviour
    {
        private const string BallTag = "Ball";
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(BallTag) && other.TryGetComponent(out CommonBall commonBall))
            {
                if (commonBall.IsSnapped)
                {
                    Debug.Log($"{commonBall.Owner.name}의 공이 게임 종료 영역에 들어왔습니다.");
                }
                else
                {
                    Debug.Log("🚨 GameEndArea: 공이 게임 종료 영역에 들어왔지만 아직 그리드에 스냅되지 않았습니다.");
                }
            }
            else if(other.CompareTag(BallTag))
                Debug.Log("🚨 GameEndArea: 공이 게임 종료 영역에 들어왔지만 CommonBall 컴포넌트를 찾을 수 없습니다.");
            else if(other.TryGetComponent(out CommonBall _))
                Debug.Log("🚨 GameEndArea: 공이 게임 종료 영역에 들어왔지만 Ball 태그가 없습니다.");
        }
    }
}