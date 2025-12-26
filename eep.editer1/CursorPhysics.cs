using System;
using System.Drawing;

namespace eep.editer1
{
    public class CursorPhysics
    {
        // ==========================================
        // 定数設定 (10ms = 1.0dt 基準のパラメータのまま)
        // ==========================================
        private const float Y_SMOOTH = 0.3f;
        private const float X_TENSION = 0.15f;
        private const float RAPID_TENSION = 0.02f;
        private const float RAPID_FRICTION = 0.85f;
        private const float FRICTION_FORWARD = 0.65f;
        private const float FRICTION_BACKWARD = 0.45f;
        private const float SNAP_THRESHOLD = 0.5f;
        private const float STOP_VELOCITY = 0.5f;

        // ==========================================
        // 状態変数
        // ==========================================
        public float PosX { get; private set; }
        public float PosY { get; private set; }
        private float velX = 0;
        private float maxTargetX = 0;

        /// <summary>
        /// 物理演算の更新ステップを実行します
        /// </summary>
        /// <param name="deltaTime">基準時間(10ms)に対する倍率。10ms経過なら1.0</param>
        public void Update(Point realTargetPos, bool isTyping, bool isDeleting, float ratchetThreshold, float deltaTime)
        {
            float effectiveTargetX = realTargetPos.X;

            // --- ラチェット機構 (変更なし) ---
            if (isTyping && !isDeleting)
            {
                if (realTargetPos.X >= maxTargetX)
                {
                    maxTargetX = realTargetPos.X;
                    effectiveTargetX = realTargetPos.X;
                }
                else
                {
                    float jumpDistance = maxTargetX - realTargetPos.X;
                    if (jumpDistance < ratchetThreshold) effectiveTargetX = maxTargetX;
                    else
                    {
                        maxTargetX = realTargetPos.X;
                        effectiveTargetX = realTargetPos.X;
                    }
                }
            }
            else
            {
                maxTargetX = realTargetPos.X;
                effectiveTargetX = realTargetPos.X;
            }

            // --- 物理演算 (DeltaTime対応) ---

            // 1. Y軸 (Lerp相当の処理)
            // dt倍することで、時間が遅れた分だけ大きく進む
            PosY += (realTargetPos.Y - PosY) * Y_SMOOTH * deltaTime;

            // 2. X軸
            float diffX = effectiveTargetX - PosX;
            float diffY = Math.Abs(realTargetPos.Y - PosY);

            if (diffY > 5.0f)
            {
                PosX += diffX * 0.3f * deltaTime; // Yがズレたときの緊急追従もdt対応
                velX = 0;
            }
            else if (Math.Abs(diffX) < SNAP_THRESHOLD && Math.Abs(velX) < STOP_VELOCITY)
            {
                PosX = effectiveTargetX;
                velX = 0;
            }
            else
            {
                float tension;
                float friction;
                bool isMovingLeft = (diffX < 0);

                if (isTyping && !isMovingLeft)
                {
                    tension = RAPID_TENSION;
                    friction = RAPID_FRICTION;
                }
                else if (isMovingLeft)
                {
                    tension = X_TENSION;
                    friction = FRICTION_BACKWARD;
                }
                else
                {
                    tension = X_TENSION;
                    friction = FRICTION_FORWARD;
                }

                // --- 運動方程式の適用 ---

                // 加速: F = ma (今回はm=1想定)。速度変化は 力 * 時間
                float force = diffX * tension;
                velX += force * deltaTime;

                // 摩擦: 減衰係数の時間乗。
                // 例: 0.85 (1回分) -> 10ms経過なら 0.85^1
                //     20ms経過なら 0.85^2 = 0.7225 (2回分減衰)
                velX *= (float)Math.Pow(friction, deltaTime);

                // 位置変化: 速度 * 時間
                PosX += velX * deltaTime;
            }
        }
    }
}