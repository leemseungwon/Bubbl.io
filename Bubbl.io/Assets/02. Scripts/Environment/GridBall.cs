
using System;
using System.Collections;
using System.Collections.Generic;
using _02._Scripts.Core.Systems;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _02._Scripts.Environment
{
    public class GridBall : MonoBehaviour
    {
        [SerializeField] private User user;
        [SerializeField] private Entity.Entity owner;

        [SerializeField] private float ballRadius = 0.5f;
        [SerializeField] private int startLowGridRow = 3;

        [Header("Auto Lower Settings")] [SerializeField]
        private float autoLowerInterval = 30f;

        private float _timer;
        public bool IsAutoLowering { get; set; } = true;
        public event Action OnLowerGrid;
        public event Action<List<Ball.CommonBall>> OnBallsPopped;

        [Header("Debug Settings")]
        [SerializeField] private bool showDebugGrid = true;

        [SerializeField] private int debugRows = 14;
        [SerializeField] private int maxCols = 5;
        [SerializeField] private Color gizmoColor = Color.cyan;

        private BallSystem _ballSystem;
        private float _ballDiameter;
        private float _rowHeight;

        private Dictionary<Vector2Int, Ball.CommonBall> _gridMatrix = new Dictionary<Vector2Int, Ball.CommonBall>();
        private List<Ball.CommonBall> _snappedBalls = new List<Ball.CommonBall>();

        private Vector3 _initialTrueCeiling;
        private Vector3 _absoluteOrigin;
        private int _loweredCount = 0;

        private readonly Vector2Int[] _evenRowNeighbors =
        {
            new Vector2Int(-1, 0), new Vector2Int(1, 0),
            new Vector2Int(-1, -1), new Vector2Int(0, -1),
            new Vector2Int(-1, 1), new Vector2Int(0, 1)
        };

        private readonly Vector2Int[] _oddRowNeighbors =
        {
            new Vector2Int(-1, 0), new Vector2Int(1, 0),
            new Vector2Int(0, -1), new Vector2Int(1, -1),
            new Vector2Int(0, 1), new Vector2Int(1, 1)
        };

        private GameSystem _gameSystem;
        private const int EndGameY = 10;
        private bool _isEnd = false;

        private void Awake()
        {
            _ballDiameter = ballRadius * 2f;
            _rowHeight = _ballDiameter * 0.8660254f;

            _absoluteOrigin = transform.position;
            _initialTrueCeiling = transform.position;
            _loweredCount = 0;
            _timer = 0;
        }

        private void Start()
        {
            _gameSystem = Core.Systems.System.Instance.GetSystem<GameSystem>();
            _ballSystem = Core.Systems.System.Instance.GetSystem<BallSystem>();

            if (startLowGridRow > 0)
            {
                for (int r = 0; r < startLowGridRow; r++)
                {
                    FillRowWithNewBalls(r, checkMatch: false);
                }
            }
        }

        private void Update()
        {
            if (_isEnd)
                return;

            if (!IsAutoLowering)
                return;

            _timer += Time.deltaTime;

            if (_timer >= autoLowerInterval)
            {
                _timer = 0f;
                LowerGrid();
            }
            
            if (CheckEndRowBall())
            {
                _gameSystem.GameEnd(user);
            }
        }

        public void LowerGrid(int low = 1, bool isRandomColor = true)
        {
            if(_isEnd)
                return;
            
            if (low <= 0) 
                return;

            IsAutoLowering = false;
            
            _loweredCount += low;
            
            _absoluteOrigin += new Vector3(0f, -_rowHeight * low, 0f);

            foreach (var pair in _gridMatrix)
            {
                Vector2Int targetGridPos = pair.Key;
                Ball.CommonBall ball = pair.Value;
                if (ball == null) 
                    continue;
                
                if (ball.GetComponent<Rigidbody>().isKinematic == false)
                    continue;

                ball.transform.position = GridToWorld(targetGridPos);
            }
            
            for (int r = 1; r <= low; r++)
            {
                int newCeilingRow = -_loweredCount + (low - r);
                FillRowWithNewBalls(newCeilingRow, checkMatch: false);
            }
            
            IsAutoLowering = true;
            OnLowerGrid?.Invoke();
        }

        private bool CheckEndRowBall()
        {
            foreach (var pair in _gridMatrix)
            {
                Vector2Int gridPos = pair.Key;
                Ball.CommonBall ball = pair.Value;
                if (ball == null) 
                    continue;
                
                if (gridPos.y == EndGameY)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsRowOdd(int row)
        {
            return (row & 1) != 0;
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float xOffset = IsRowOdd(gridPos.y) ? ballRadius : 0f;
            float xPos = _absoluteOrigin.x + (gridPos.x * _ballDiameter) + xOffset;
            float yPos = _absoluteOrigin.y - (gridPos.y * _rowHeight);

            return new Vector3(xPos, yPos, _absoluteOrigin.z);
        }
        
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float relativeX = worldPos.x - _absoluteOrigin.x;
            float relativeY = _absoluteOrigin.y - worldPos.y;

            // 행(Row) 계산 시, 아래로 내려갈수록 y값이 커짐
            int row = Mathf.RoundToInt(relativeY / _rowHeight);

            float xOffset = IsRowOdd(row) ? ballRadius : 0f;
            // xOffset을 고려하여 열(Col) 계산
            int col = Mathf.RoundToInt((relativeX - xOffset) / _ballDiameter);

            return new Vector2Int(col, row);
        }

        public void SnapToGrid(Ball.CommonBall ball, bool checkMatch = true)
        {
            if (ball == null) return;

            Vector2Int gridPos = WorldToGrid(ball.transform.position);
            // 좌표 보정: 그리드 영역 강제 클램핑
            gridPos.x = Mathf.Clamp(gridPos.x, 0, IsRowOdd(gridPos.y) ? maxCols - 1 : maxCols);
            gridPos.y = Mathf.Max(gridPos.y, -_loweredCount); 

            while (_gridMatrix.ContainsKey(gridPos)) gridPos.y += 1;

            _gridMatrix[gridPos] = ball;
            ball.transform.position = GridToWorld(gridPos);
    
            if (checkMatch) CheckAndPopMatch(gridPos);
        }

        private void FillRowWithNewBalls(int targetRow, bool checkMatch = false)
        {
            if (_ballSystem == null) return;
            
            int colsToFill = IsRowOdd(targetRow) ? maxCols - 1 : maxCols;
            
            for (int col = 0; col < colsToFill; col++)
            {
                Vector2Int gridPos = new Vector2Int(col, targetRow);
                Vector3 spawnWorldPos = GridToWorld(gridPos);

                Entity.BallColorType randomColor = (Entity.BallColorType)Random.Range(0, System.Enum.GetValues(typeof(Entity.BallColorType)).Length);
                Ball.CommonBall newBall = _ballSystem.CreateBall(owner, randomColor);

                if (newBall != null)
                {
                    newBall.transform.position = spawnWorldPos;
                    
                    if (newBall.TryGetComponent<Rigidbody>(out var rb))
                    {
                        rb.isKinematic = true;
                        rb.velocity = Vector3.zero;
                    }

                    SnapToGrid(newBall, checkMatch);
                }
            }
        }

        public void CheckAndPopMatch(Vector2Int startGridPos)
        {
            if (!_gridMatrix.TryGetValue(startGridPos, out Ball.CommonBall startBall) || startBall == null) 
                return;

            Entity.BallColorType targetColor = startBall.Color;
            
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            List<Ball.CommonBall> matchedBalls = new List<Ball.CommonBall>();

            queue.Enqueue(startGridPos);
            visited.Add(startGridPos);
            matchedBalls.Add(startBall);

            while (queue.Count > 0)
            {
                Vector2Int currentPos = queue.Dequeue();
                Vector2Int[] neighbors = IsRowOdd(currentPos.y) ? _oddRowNeighbors : _evenRowNeighbors;

                foreach (Vector2Int offset in neighbors)
                {
                    Vector2Int neighborPos = currentPos + offset;

                    if (visited.Contains(neighborPos)) continue;

                    if (_gridMatrix.TryGetValue(neighborPos, out Ball.CommonBall neighborBall) && neighborBall != null)
                    {
                        if (neighborBall.Color == targetColor)
                        {
                            visited.Add(neighborPos);
                            queue.Enqueue(neighborPos);
                            matchedBalls.Add(neighborBall);
                        }
                    }
                }
            }

            if (matchedBalls.Count >= 3)
            {
                PopBalls(matchedBalls);
            }
        }

        private void PopBalls(List<Ball.CommonBall> ballsToPop)
        {
            foreach (var ball in ballsToPop)
            {
                if (ball == null) continue;
                RemoveBall(ball);
                ball.DestroyBall();  
            }
            
            OnBallsPopped?.Invoke(ballsToPop);

            CheckAndDropFloatingBalls();
        }

        public void CheckAndDropFloatingBalls()
        {
            HashSet<Vector2Int> connectedToCeiling = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            foreach (var pair in _gridMatrix)
            {
                Vector2Int pos = pair.Key;
                if (pair.Value != null && IsCeilingRow(pos.y))
                {
                    queue.Enqueue(pos);
                    connectedToCeiling.Add(pos);
                }
            }

            while (queue.Count > 0)
            {
                Vector2Int currentPos = queue.Dequeue();
                Vector2Int[] neighbors = IsRowOdd(currentPos.y) ? _oddRowNeighbors : _evenRowNeighbors;

                foreach (Vector2Int offset in neighbors)
                {
                    Vector2Int neighborPos = currentPos + offset;

                    if (connectedToCeiling.Contains(neighborPos)) continue;
                    if (!_gridMatrix.TryGetValue(neighborPos, out Ball.CommonBall neighborBall) ||
                        neighborBall == null) continue;

                    connectedToCeiling.Add(neighborPos);
                    queue.Enqueue(neighborPos);
                }
            }

            List<Vector2Int> floatingPositions = new List<Vector2Int>();
            foreach (var pair in _gridMatrix)
            {
                if (pair.Value != null && !connectedToCeiling.Contains(pair.Key))
                {
                    floatingPositions.Add(pair.Key);
                }
            }

            foreach (Vector2Int floatPos in floatingPositions)
            {
                Ball.CommonBall ball = _gridMatrix[floatPos];
                if (ball == null) continue;

                _gridMatrix.Remove(floatPos);
                if (_snappedBalls.Contains(ball))
                    _snappedBalls.Remove(ball);

                ball.SetFallingMode();

                if (ball.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.AddForce(new Vector3(Random.Range(-1f, 1f), -1f, 0f), ForceMode.Impulse);
                }

                StartCoroutine(DestroyDroppedBallAfterTime(ball, 2f));
            }
        }

        private bool IsCeilingRow(int row)
        {
            return row <= -_loweredCount;
        }
        
        public float[] GetGridObservations(int maxRows = 12)
        {
            List<float> observations = new List<float>();

            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < maxCols + 1; c++)
                {
                    Vector2Int pos = new Vector2Int(c, r);
            
                    // _gridMatrix에 해당 위치의 공이 있는지 확인
                    if (_gridMatrix.TryGetValue(pos, out var ball) && ball != null)
                    {
                        // 색상 값을 1~5로 변환하여 추가 (0은 비어있음으로 사용)
                        observations.Add((float)ball.Color + 1f); 
                    }
                    else
                    {
                        // 빈 칸은 0
                        observations.Add(0f);
                    }
                }
            }
            return observations.ToArray();
        }

        private IEnumerator DestroyDroppedBallAfterTime(Ball.CommonBall ball, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (ball != null)
            {
                var poolingManager = Core.Managers.Manager.Instance.GetManager<Core.Managers.PoolingManager>();
                if (poolingManager != null)
                {
                    poolingManager.DespawnPool("Ball", ball);
                }
            }
        }

        public void RemoveBall(Ball.CommonBall ball)
        {
            if (_snappedBalls.Contains(ball)) _snappedBalls.Remove(ball);
            
            Vector2Int targetKey = Vector2Int.down;
            foreach (var pair in _gridMatrix)
            {
                if (pair.Value == ball)
                {
                    targetKey = pair.Key;
                    break;
                }
            }
            if (targetKey != Vector2Int.down) _gridMatrix.Remove(targetKey);
        }

        public void OnDestroy()
        {
            _snappedBalls.Clear();
            _gridMatrix.Clear();
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGrid) return;
            
            Vector3 drawingOrigin = Application.isPlaying ? _initialTrueCeiling : transform.position;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(drawingOrigin, 0.15f);

            _ballDiameter = ballRadius * 2f;
            _rowHeight = _ballDiameter * 0.8660254f;

            int totalRowsToDraw = debugRows + (Application.isPlaying ? _loweredCount : 0);

            for (int i = 0; i < totalRowsToDraw; i++)
            {
                int virtualRowIndex = Application.isPlaying ? (i - _loweredCount) : i;
                bool isOdd = IsRowOdd(virtualRowIndex);

                int currentLineCols = isOdd ? maxCols - 1 : maxCols;
                for (int col = 0; col < currentLineCols; col++)
                {
                    float xOffset = isOdd ? ballRadius : 0f;
                    float xPos = drawingOrigin.x + (col * _ballDiameter) + xOffset;
                    float yPos = drawingOrigin.y - (i * _rowHeight);
                    Vector3 worldPos = new Vector3(xPos, yPos, drawingOrigin.z);

                    Gizmos.color = gizmoColor;
                    Gizmos.DrawWireSphere(worldPos, ballRadius);
                    Gizmos.color = isOdd ? Color.yellow : Color.cyan;
                    Gizmos.DrawSphere(worldPos, ballRadius * 0.1f);
                }
            }
        }

        public void StopAll()
        {
            _isEnd = true;
        }
        
        public void ResetGrid()
        {
            foreach (var ball in _gridMatrix.Values) if (ball != null) ball.DestroyBall();
            _gridMatrix.Clear();
            _snappedBalls.Clear();
            _loweredCount = 0;
            _absoluteOrigin = _initialTrueCeiling;
            IsAutoLowering = true;
        }

        public float GetLowestRowNormalized()
        {
            int lowest = 0;
            foreach (var pos in _gridMatrix.Keys)
            {
                if (pos.y > lowest) lowest = pos.y;
            }
            // 최대 14행 기준으로 정규화
            return Mathf.Clamp01((float)lowest / 14f);
        }
    }
}