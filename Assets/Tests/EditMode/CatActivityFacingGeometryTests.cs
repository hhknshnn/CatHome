using NUnit.Framework;
using UnityEngine;

/// <summary>Pure staging geometry; actual room contacts and rendered poses are covered by native audits.</summary>
public sealed class CatActivityFacingGeometryTests
{
    // Representative near/far and floor/perch positions in the shared room coordinate system.
    // These are geometry fixtures, not a claim that the eight authored scenes were played.
    static readonly Vector3[] RoomPoints =
    {
        new Vector3(-2.1f, .05f, -1.4f), new Vector3(0f, .05f, -1.4f),
        new Vector3(2.1f, .05f, -1.4f), new Vector3(-2.1f, .45f, .5f),
        new Vector3(2.1f, .45f, .5f), new Vector3(-1.7f, 1.15f, 2.1f),
        new Vector3(0f, 1.15f, 2.1f), new Vector3(1.7f, 1.15f, 2.1f)
    };

    [Test]
    public void FreePose_AllRoomSidesAndHeadings_AreReadableWithoutTurningAnAlreadyGoodPose()
    {
        Vector3 camera = HomeRoomCameraProfile.Position;
        float designMargin = Mathf.Cos(65f * Mathf.Deg2Rad);
        foreach (Vector3 position in RoomPoints)
        for (int yaw = 0; yaw < 360; yaw += 5)
        {
            Quaternion preferred = Quaternion.Euler(0f, yaw, 0f);
            Quaternion result = CatActivityFacing.Resolve(position, preferred, camera);
            string sample = position + ", yaw=" + yaw;
            float before = PlanarViewDot(preferred * Vector3.forward, position, camera);
            float after = PlanarViewDot(result * Vector3.forward, position, camera);
            Assert.That(after, Is.GreaterThanOrEqualTo(designMargin - .0001f), sample);
            Assert.That(after, Is.GreaterThanOrEqualTo(CatActivityFacing.MinimumViewDot), sample);
            // A torso is ahead of its movement root. Check the visible-side
            // contract at a representative body midpoint as well as the pivot.
            Vector3 torsoMidpoint = position + result * new Vector3(0f, .30f, .45f);
            Assert.That(PlanarViewDot(result * Vector3.forward, torsoMidpoint, camera),
                Is.GreaterThanOrEqualTo(CatActivityFacing.MinimumViewDot), sample + ", torso midpoint");
            Assert.That(Vector3.Angle(result * Vector3.up, Vector3.up), Is.LessThan(.05f), sample);
            if (before > designMargin + .0001f)
                Assert.That(Quaternion.Angle(preferred, result), Is.LessThan(.06f),
                    "A readable natural pose should not acquire an unnecessary camera turn: " + sample);
        }
    }

    [Test]
    public void NarrowSupport_AllHeadings_SelectOnlyAnAxisEnd_AndPreserveTheTiltedSupportPlane()
    {
        Vector3 camera = HomeRoomCameraProfile.Position;
        foreach (Vector3 position in RoomPoints)
        foreach (float pitch in new[] { -18f, 0f, 18f })
        foreach (float roll in new[] { -12f, 0f, 12f })
        for (int yaw = 0; yaw < 360; yaw += 15)
        {
            Quaternion preferred = Quaternion.Euler(pitch, yaw, roll);
            Quaternion result = CatActivityFacing.AlongAxis(position, preferred, camera);
            string sample = position + ", support=" + new Vector3(pitch, yaw, roll);
            Vector3 originalAxis = preferred * Vector3.forward;
            Vector3 selectedAxis = result * Vector3.forward;
            Assert.That(Mathf.Abs(Vector3.Dot(originalAxis, selectedAxis)), Is.GreaterThan(.99999f),
                "A narrow seat cannot accept a sideways body rotation: " + sample);
            Assert.That(Vector3.Angle(result * Vector3.up, preferred * Vector3.up), Is.LessThan(.06f),
                "Selecting the opposite end must preserve the support's tilt: " + sample);
            float turn = Quaternion.Angle(preferred, result);
            Assert.That(Mathf.Min(turn, Mathf.Abs(180f - turn)), Is.LessThan(.06f), sample);
            Assert.That(PlanarViewDot(selectedAxis, position, camera),
                Is.GreaterThanOrEqualTo(CatActivityFacing.MinimumViewDot), sample);
            if (PlanarViewDot(originalAxis, position, camera) > .001f)
                Assert.That(turn, Is.LessThan(.06f), "Keep the already visible support end: " + sample);
        }
    }

    [Test]
    public void FacingPolicy_UsesTheSuppliedCameraAndHorizontalGeometry_NotWorldForwardOrHeight()
    {
        Vector3 camera = HomeRoomCameraProfile.Position;
        Quaternion roomTurn = Quaternion.Euler(0f, 137f, 0f);
        Vector3 roomOffset = new Vector3(11f, 7f, -4f);
        foreach (Vector3 position in RoomPoints)
        for (int yaw = 0; yaw < 360; yaw += 15)
        {
            Quaternion preferred = Quaternion.Euler(0f, yaw, 0f);
            Quaternion original = CatActivityFacing.Resolve(position, preferred, camera);
            Quaternion moved = CatActivityFacing.Resolve(roomTurn * position + roomOffset,
                roomTurn * preferred, roomTurn * camera + roomOffset);
            Assert.That(Quaternion.Angle(roomTurn * original, moved), Is.LessThan(.08f),
                "A camera-relative room transform must carry the free pose with it.");
            Quaternion raisedCamera = CatActivityFacing.Resolve(position, preferred, camera + Vector3.up * 9f);
            Assert.That(Quaternion.Angle(original, raisedCamera), Is.LessThan(.06f),
                "Camera elevation must not change the horizontal facing decision.");

            Quaternion originalAxis = CatActivityFacing.AlongAxis(position, preferred, camera);
            Quaternion movedAxis = CatActivityFacing.AlongAxis(roomTurn * position + roomOffset,
                roomTurn * preferred, roomTurn * camera + roomOffset);
            // Exactly side-on ends tie; either end is physically valid in that singular case.
            if (Mathf.Abs(PlanarViewDot(preferred * Vector3.forward, position, camera)) > .001f)
                Assert.That(Quaternion.Angle(roomTurn * originalAxis, movedAxis), Is.LessThan(.08f),
                    "The visible support end must follow the supplied camera.");
        }
    }

    static float PlanarViewDot(Vector3 bodyForward, Vector3 position, Vector3 camera)
    {
        Vector3 forward = Vector3.ProjectOnPlane(bodyForward, Vector3.up).normalized;
        Vector3 view = Vector3.ProjectOnPlane(camera - position, Vector3.up).normalized;
        Assert.That(forward.sqrMagnitude, Is.GreaterThan(.99f), "The fixture must contain a measurable body axis.");
        Assert.That(view.sqrMagnitude, Is.GreaterThan(.99f), "The fixture must contain a measurable camera direction.");
        return Vector3.Dot(forward, view);
    }
}
