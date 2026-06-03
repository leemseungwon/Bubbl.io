using System;
using System.Collections.Generic;
using _02._Scripts.Ball;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace _02._Scripts.Entity.Player
{
    public class EnemyAgent : Agent
    {
        [SerializeField] private Enemy enemy;
        
        [Header("Agent Settings")]
        [SerializeField] private float maxAimAngle = 80f;
        [SerializeField] private float timePenalty = -0.001f;
        [SerializeField] private float snapReward = 0.5f;
        [SerializeField] private bool useInstantAim = true;

        [Header("Optional Bounds (for obs normalization)")]
        [SerializeField] private Vector2 areaSize = new Vector2(10f, 10f);

        private HandleBallComponent _handleBall;
        private CommonBall _launchedBallWaitingForSnap = null;

        public override void OnEpisodeBegin()
        {
            if (enemy != null)
            {
                _handleBall = enemy.GetCompo<HandleBallComponent>();
            }
            
            if (_launchedBallWaitingForSnap != null)
            {
                try
                {
                    _launchedBallWaitingForSnap.OnSnapToGrid -= OnLaunchedBallSnapped;
                }
                catch { }
            }
            _launchedBallWaitingForSnap = null;
    
            // GridBall의 팝 이벤트 구독 (매칭되어 사라질 때 감지)
            if (enemy != null && enemy.OwnGrid != null)
            {
                enemy.OwnGrid.OnBallsPopped += OnBallsPopped;
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // 1) 현재 발사될 공의 색상(One-hot)
            int colorCount = System.Enum.GetNames(typeof(BallColorType)).Length;
            if (_handleBall != null && _handleBall.CurrentBall != null)
            {
                BallColorType color = _handleBall.CurrentBall.Color;
                for (int i = 0; i < colorCount; i++)
                {
                    sensor.AddObservation(i == (int)color ? 1f : 0f);
                }
            }
            else
            {
                for (int i = 0; i < colorCount; i++) sensor.AddObservation(0f);
            }

            // 2) 발사 pivot의 방향 (transform.up x,y)
            Vector3 up = transform.up;
            sensor.AddObservation(up.x);
            sensor.AddObservation(up.y);

            // 3) 위치 정규화
            sensor.AddObservation(transform.position.x / Mathf.Max(areaSize.x, 0.001f));
            sensor.AddObservation(transform.position.y / Mathf.Max(areaSize.y, 0.001f));

            // 4) 발사 가능 여부
            bool hasBall = (_handleBall != null && _handleBall.CurrentBall != null);
            sensor.AddObservation(hasBall ? 1f : 0f);
            
            if (enemy != null && enemy.OwnGrid != null)
            {
                float[] gridData = enemy.OwnGrid.GetGridObservations();
                foreach (float val in gridData)
                {
                    sensor.AddObservation(val);
                }
            }
        }

        // Actions: continuous[0]=aim, continuous[1]=fire
        public override void OnActionReceived(ActionBuffers actions)
        {
            var cont = actions.ContinuousActions;
            float aimInput = Mathf.Clamp(cont[0], -1f, 1f);
            float fireInput = Mathf.Clamp(cont[1], -1f, 1f);

            float targetAngle = aimInput * maxAimAngle;
            Vector3 desiredDir = Quaternion.Euler(0f, 0f, targetAngle) * Vector3.up;
            if (useInstantAim) transform.up = desiredDir;
            else transform.up = Vector3.Lerp(transform.up, desiredDir, 0.2f);

            bool hasBall = (_handleBall != null && _handleBall.CurrentBall != null);

            // 발사 조건: fire 신호 + 공 있음 + 현재 발사 대기 중인 공 없음
            if (fireInput > 0.5f && hasBall && _launchedBallWaitingForSnap == null)
            {
                // 발사 전에 이전 공의 이벤트가 남아있으면 해제
                if (_launchedBallWaitingForSnap != null)
                {
                    try
                    {
                        _launchedBallWaitingForSnap.OnSnapToGrid -= OnLaunchedBallSnapped;
                    }
                    catch { }
                }

                // 공 발사
                _handleBall.LaunchCurrentBall(new Vector2(transform.up.x, transform.up.y));
                
                // 발사한 공을 추적
                _launchedBallWaitingForSnap = _handleBall.CurrentBall;
                if (_launchedBallWaitingForSnap != null)
                {
                    _launchedBallWaitingForSnap.OnSnapToGrid += OnLaunchedBallSnapped;
                }
                
                AddReward(0.1f);
            }

            AddReward(timePenalty);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var cont = actionsOut.ContinuousActions;
            float aim = 0f;
            if (Input.GetKey(KeyCode.LeftArrow)) aim = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) aim = 1f;
            cont[0] = aim;
            cont[1] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
        }

        private void OnLaunchedBallSnapped()
        {
            // 공이 스냅되면: 보상 주고, 구독 해제, 상태 정리
            AddReward(snapReward);
            
            if (_launchedBallWaitingForSnap != null)
            {
                try
                {
                    _launchedBallWaitingForSnap.OnSnapToGrid -= OnLaunchedBallSnapped;
                }
                catch { }
            }
            
            _launchedBallWaitingForSnap = null;
        }
        
        private void OnBallsPopped(List<CommonBall> poppedBalls)
        {
            // 발사한 공이 팝되었으면 상태 초기화해서 다음 발사 가능하게
            if (_launchedBallWaitingForSnap != null && poppedBalls.Contains(_launchedBallWaitingForSnap))
            {
                AddReward(snapReward); // 매칭 성공 보상
                _launchedBallWaitingForSnap = null;
            }
        }

        private void OnDestroy()
        {
            // OnSnapToGrid 구독 해제
            if (_launchedBallWaitingForSnap != null)
            {
                try
                {
                    _launchedBallWaitingForSnap.OnSnapToGrid -= OnLaunchedBallSnapped;
                }
                catch { }
            }
    
            // OnBallsPopped 구독 해제
            if (enemy != null && enemy.OwnGrid != null)
            {
                try
                {
                    enemy.OwnGrid.OnBallsPopped -= OnBallsPopped;
                }
                catch { }
            }
        }
    }
}