using UnityEngine;

namespace Game.Enemies.Fighter
{
    public class MoveToWaypointState : IJetState
    {
        public void Enter(FighterJetController jet)
        {
        }

        public void Update(FighterJetController jet)
        {
            var waypoints = jet.Waypoints;

            if (waypoints == null || waypoints.Count == 0)
            {
                jet.ChangeState(new MoveToExitState());
                return;
            }

            if (jet.CurrentWaypointIndex >= waypoints.Count)
            {
                jet.ChangeState(new MoveToExitState());
                return;
            }

            FlightWaypoint waypoint =
                waypoints[jet.CurrentWaypointIndex];

            Transform target = waypoint.Point;

            if (target == null)
            {
                AdvanceToNext(jet);
                return;
            }

            Transform movement = jet.MovementTransform;

            float distance =
                Vector3.Distance(
                    movement.position,
                    target.position
                );

            if (waypoint.RotateTowardTarget)
            {
                // حالت عادی: فقط بر اساس مسیر رسیدن می‌چرخه و مستقیم
                // سمت موقعیت target می‌ره. کاری به rotation خودِ
                // ترنسفورم نداره - اون کار فاز بعدیه (AlignAtWaypoint).
                MoveAndRotateTowards(
                    jet,
                    target.position,
                    SpeedMultiplierForDistance(jet, distance)
                );

                if (distance <= jet.StoppingDistance)
                {
                    // اول موقعیتش رسیده - حالا برو یه فاز جدا که فقط
                    // سرجاش می‌چرخه تا rotation دقیق target رو بگیره.
                    // این کار رو همزمان با حرکت قاطی نمی‌کنیم چون باعث
                    // ناپایداری و دور خودش چرخیدن می‌شد.
                    jet.ChangeState(
                        new AlignAtWaypointState()
                    );
                }
            }
            else
            {
                // حالت "نچرخ": rotation دست نمی‌خوره، فقط موقعیت مستقیم
                // (خط راست) به سمت target می‌ره.
                MoveWithoutRotating(
                    jet,
                    target.position,
                    SpeedMultiplierForDistance(jet, distance)
                );

                if (distance <= jet.StoppingDistance)
                {
                    CompleteWaypoint(jet, waypoint);
                }
            }
        }

        public void Exit(FighterJetController jet)
        {
        }

        public static void AdvanceToNext(FighterJetController jet)
        {
            jet.CurrentWaypointIndex++;

            if (jet.CurrentWaypointIndex < jet.Waypoints.Count)
            {
                jet.ChangeState(
                    new MoveToWaypointState()
                );
            }
            else
            {
                jet.ChangeState(
                    new MoveToExitState()
                );
            }
        }

        public static void CompleteWaypoint(
            FighterJetController jet,
            FlightWaypoint waypoint)
        {
            if (waypoint.WaitTime <= 0f)
            {
                // Nothing to wait for - keep the jet's current speed and
                // heading and go straight to the next point instead of
                // bleeding speed toward zero.
                AdvanceToNext(jet);
            }
            else
            {
                jet.WaitTimer = waypoint.WaitTime;

                jet.ChangeState(
                    new WaitAtWaypointState()
                );
            }
        }

        public static float SpeedMultiplierForDistance(
            FighterJetController jet,
            float distance)
        {
            if (jet.SlowdownDistance <= 0.01f)
                return 1f;

            return Mathf.Clamp(
                distance / jet.SlowdownDistance,
                jet.MinForwardSpeedFraction,
                1f
            );
        }

        // جت به سمت موقعیت target می‌چرخه (نرم، با RotationSpeed) و روی
        // جهت واقعی دماغه‌اش حرکت می‌کنه. فقط بر اساس مسیر رسیدنه - کاری
        // به rotation خودِ ترنسفورم target نداره.
        public static void MoveAndRotateTowards(
            FighterJetController jet,
            Vector3 targetPosition,
            float speedMultiplier = 1f)
        {
            Transform t = jet.MovementTransform;

            if (t == null)
                return;

            Vector3 toTarget = targetPosition - t.position;

            float turnAmount = 0f;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Vector3 direction = toTarget.normalized;

                Quaternion desiredRotation =
                    Quaternion.LookRotation(direction, Vector3.up);

                turnAmount =
                    Vector3.SignedAngle(
                        t.forward,
                        direction,
                        Vector3.up
                    );

                float rotT =
                    1f -
                    Mathf.Exp(
                        -jet.RotationSpeed *
                        Time.deltaTime
                    );

                t.rotation =
                    Quaternion.Slerp(
                        t.rotation,
                        desiredRotation,
                        rotT
                    );
            }

            AdvanceSpeedAndPosition(jet, speedMultiplier);

            ApplyBank(jet, turnAmount);
        }

        // جت اصلاً نمی‌چرخه - rotation دست‌نخورده می‌مونه - ولی موقعیتش
        // مستقیم (خط راست) به سمت target می‌ره. برای waypoint هایی که
        // RotateTowardTarget = false دارن.
        public static void MoveWithoutRotating(
            FighterJetController jet,
            Vector3 targetPosition,
            float speedMultiplier = 1f)
        {
            Transform t = jet.MovementTransform;

            if (t == null)
                return;

            AdvanceSpeed(jet, speedMultiplier);

            Vector3 toTarget = targetPosition - t.position;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Vector3 direction = toTarget.normalized;

                t.position +=
                    direction *
                    jet.CurrentSpeed *
                    Time.deltaTime;
            }

            ApplyBank(jet, 0f);
        }

        private static void AdvanceSpeed(
            FighterJetController jet,
            float speedMultiplier)
        {
            float targetSpeed =
                jet.MoveSpeed *
                Mathf.Clamp01(speedMultiplier);

            float rate =
                targetSpeed > jet.CurrentSpeed
                    ? jet.Acceleration
                    : jet.Deceleration;

            jet.CurrentSpeed =
                Mathf.MoveTowards(
                    jet.CurrentSpeed,
                    targetSpeed,
                    rate * Time.deltaTime
                );
        }

        private static void AdvanceSpeedAndPosition(
            FighterJetController jet,
            float speedMultiplier)
        {
            Transform t = jet.MovementTransform;

            AdvanceSpeed(jet, speedMultiplier);

            t.position +=
                t.forward *
                jet.CurrentSpeed *
                Time.deltaTime;
        }

        public static void ApplyBank(
            FighterJetController jet,
            float turnAmount)
        {
            Transform visual =
                jet.VisualRoot;

            // اگر VisualRoot خود MovementRoot نیست
            // بانکینگ روی مدل انجام می‌شود.
            if (visual == jet.MovementTransform)
                return;

            float targetBank =
                Mathf.Clamp(
                    -turnAmount * jet.BankFactor,
                    -jet.MaxBankAngle,
                    jet.MaxBankAngle
                );

            Quaternion targetLocalRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    targetBank
                );

            float bankT =
                1f -
                Mathf.Exp(
                    -jet.BankSmoothSpeed *
                    Time.deltaTime
                );

            visual.localRotation =
                Quaternion.Slerp(
                    visual.localRotation,
                    targetLocalRotation,
                    bankT
                );
        }
    }


    // فاز کوتاهی که بعد از رسیدن به موقعیت waypoint اجرا می‌شه - فقط
    // سرجا می‌چرخه (بدون حرکت مسیر جدید) تا دقیقاً rotation خودِ
    // ترنسفورم target رو بگیره. جدا از MoveToWaypointState نگه داشته
    // شده تا "رفتن سمت موقعیت" و "چرخیدن به rotation دقیق" با هم قاطی
    // نشن - قاطی‌شدن‌شون همون چیزی بود که باعث دور خودش چرخیدن می‌شد.
    public class AlignAtWaypointState : IJetState
    {
        public void Enter(FighterJetController jet)
        {
        }

        public void Update(FighterJetController jet)
        {
            var waypoints = jet.Waypoints;

            if (waypoints == null ||
                jet.CurrentWaypointIndex >= waypoints.Count)
            {
                jet.ChangeState(new MoveToExitState());
                return;
            }

            FlightWaypoint waypoint =
                waypoints[jet.CurrentWaypointIndex];

            Transform target = waypoint.Point;

            if (target == null)
            {
                MoveToWaypointState.AdvanceToNext(jet);
                return;
            }

            Transform movement = jet.MovementTransform;

            // سرعت رو به صفر می‌بریم - این فاز فقط برای چرخیدنه، نه
            // ادامه‌ی مسیر. هرچی مونده به آرومی طی می‌شه تا کاملاً بایسته.
            float rate = jet.Deceleration;

            jet.CurrentSpeed =
                Mathf.MoveTowards(
                    jet.CurrentSpeed,
                    0f,
                    rate * Time.deltaTime
                );

            movement.position +=
                movement.forward *
                jet.CurrentSpeed *
                Time.deltaTime;

            float rotT =
                1f -
                Mathf.Exp(
                    -jet.RotationSpeed *
                    Time.deltaTime
                );

            movement.rotation =
                Quaternion.Slerp(
                    movement.rotation,
                    target.rotation,
                    rotT
                );

            MoveToWaypointState.ApplyBank(jet, 0f);

            float angle =
                Quaternion.Angle(
                    movement.rotation,
                    target.rotation
                );

            if (angle <= jet.ArrivalAngleThreshold)
            {
                MoveToWaypointState.CompleteWaypoint(jet, waypoint);
            }
        }

        public void Exit(FighterJetController jet)
        {
        }
    }


    public class WaitAtWaypointState : IJetState
    {
        public void Enter(FighterJetController jet)
        {
        }

        public void Update(FighterJetController jet)
        {
            jet.WaitTimer -= Time.deltaTime;

            jet.CurrentSpeed =
                Mathf.MoveTowards(
                    jet.CurrentSpeed,
                    0f,
                    jet.Deceleration *
                    Time.deltaTime
                );

            if (jet.WaitTimer <= 0f)
            {
                jet.CurrentWaypointIndex++;

                if (jet.Waypoints != null &&
                    jet.CurrentWaypointIndex <
                    jet.Waypoints.Count)
                {
                    jet.ChangeState(
                        new MoveToWaypointState()
                    );
                }
                else
                {
                    jet.ChangeState(
                        new MoveToExitState()
                    );
                }
            }
        }

        public void Exit(FighterJetController jet)
        {
        }
    }


    public class MoveToExitState : IJetState
    {
        public void Enter(FighterJetController jet)
        {
        }

        public void Update(FighterJetController jet)
        {
            if (jet.ExitPoint == null)
            {
                jet.ChangeState(
                    new DespawnState()
                );

                return;
            }

            MoveToWaypointState.MoveAndRotateTowards(
                jet,
                jet.ExitPoint.position
            );

            float distance =
                Vector3.Distance(
                    jet.MovementTransform.position,
                    jet.ExitPoint.position
                );

            if (distance <= jet.StoppingDistance)
            {
                jet.ChangeState(
                    new DespawnState()
                );
            }
        }

        public void Exit(FighterJetController jet)
        {
        }
    }


    public class DespawnState : IJetState
    {
        public void Enter(FighterJetController jet)
        {
            jet.Despawn();
        }

        public void Update(FighterJetController jet)
        {
        }

        public void Exit(FighterJetController jet)
        {
        }
    }
}