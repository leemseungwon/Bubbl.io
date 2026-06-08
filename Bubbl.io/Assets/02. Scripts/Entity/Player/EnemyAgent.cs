
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
        [SerializeField] private float fireCooldown = 0.5f; // 0.5초 대기
        [SerializeField] private float maxAimAngle = 80f;
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float popReward = 0.5f;
        [SerializeField] private bool useInstantAim = true;

        [Header("Optional Bounds (for obs normalization)")]
        [SerializeField] private Vector2 areaSize = new Vector2(10f, 10f);

        private HandleBallComponent _handleBall;
        
        private float _targetAngle = 0f;

        private bool IsEnd { get; set; }
        
        private float _lastFireTime;
        
        private static float _maxScore = 0;
        private float _score = 0;
        
        public override void OnEpisodeBegin()
        {
            if (enemy != null)
                IsEnd = false; 
    
            if (enemy != null && _handleBall == null)
            {
                _handleBall = enemy.GetCompo<HandleBallComponent>();
            }
    
            if (enemy != null && enemy.OwnGrid != null)
            {
                enemy.OwnGrid.OnBallsPopped -= OnBallsPopped; 
                enemy.OwnGrid.OnBallsPopped += OnBallsPopped;
            }
            
            ResetAgent();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (_handleBall != null && _handleBall.CurrentBall != null)
            {
                sensor.AddObservation(1.0f); // 공이 있음을 명시
                sensor.AddObservation((float)_handleBall.CurrentBall.Color / 5.0f); // 색상 정보
            }
            else
            {
                sensor.AddObservation(0.0f);
                sensor.AddObservation(0.0f);
            }
            
            float[] gridObs = enemy.OwnGrid.GetGridObservations(12);
            foreach (float val in gridObs) sensor.AddObservation(val);
            
            sensor.AddObservation(enemy.OwnGrid.GetLowestRowNormalized());
        }
        
        private void Update()
        {
            if(IsEnd)
                return;
            
            if (useInstantAim)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, _targetAngle);
                return;
            }

            float currentZ = transform.eulerAngles.z;
            float nextZ = Mathf.MoveTowardsAngle(currentZ, _targetAngle, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, nextZ);
        }

        // Actions: continuous[0]=aim, continuous[1]=fire
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (IsEnd || _handleBall == null) return;

            var cont = actions.ContinuousActions;
            float aimInput = Mathf.Clamp(cont[0], -1f, 1f);
            float fireInput = cont[1]; // -1 ~ 1

            // 조준은 언제든 가능
            _targetAngle = aimInput * maxAimAngle;

            // 발사 로직: 공이 있을 때만 발사 시도
            if (fireInput > 0.5f)
            {
                if (_handleBall.CurrentBall != null && (Time.time - _lastFireTime) > fireCooldown)
                {
                    _handleBall.LaunchCurrentBall(transform.up);
                    _lastFireTime = Time.time;
                    _score += 0.01f;
                    AddReward(0.01f);
                }
                else if (_handleBall.CurrentBall == null)
                {
                    _score -= 0.01f;
                    AddReward(-0.01f); 
                }
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            if(IsEnd)
                return;
            
            var cont = actionsOut.ContinuousActions;
            float aim = 0f;
            if (Input.GetKey(KeyCode.LeftArrow))
                aim = -1f;
            if (Input.GetKey(KeyCode.RightArrow))
                aim = 1f;
            
            cont[0] = aim;
            cont[1] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
        }
        
        private void OnBallsPopped(List<CommonBall> poppedBalls)
        {
            _score += 5f + (poppedBalls.Count * 15f);
            AddReward(5f + (poppedBalls.Count * 15f));
        }
        
        public void End(bool isWin)
        {
            IsEnd = true;
            if (isWin)
            {
                _score += 100f;
                AddReward(100f);
            }
            else
            {
                _score -= 100f;
                AddReward(-100f);
            }
            enemy.OwnGrid.StopAll();
            
            _maxScore = Mathf.Max(_maxScore, _score);
            _score = 0;
            Debug.Log($"Max Score: {_maxScore}");
            
            ResetAgent();
        }

        private void OnDestroy()
        {
            if (enemy != null && enemy.OwnGrid != null)
            {
                enemy.OwnGrid.OnBallsPopped -= OnBallsPopped;
            }
        }

        private void ResetAgent()
        {
            IsEnd = false;
            _lastFireTime = 0f;
            
            if (enemy != null)
            {
                enemy.OwnGrid?.ResetGrid();
                _handleBall.Restart();
            }
        }
    }
}