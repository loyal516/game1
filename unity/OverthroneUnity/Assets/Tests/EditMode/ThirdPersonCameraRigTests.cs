using NUnit.Framework;
using Overthrone;
using UnityEngine;

public sealed class ThirdPersonCameraRigTests
{
    [Test]
    public void ResolveCameraPositionReturnsDesiredPositionWithoutObstacles()
    {
        var rigObject = new GameObject("Camera Rig No Obstacle");
        try
        {
            var rig = rigObject.AddComponent<ThirdPersonCameraRig>();
            var lookTarget = new Vector3(0f, 1.35f, 0f);
            var desiredPosition = new Vector3(0f, 2.2f, -4.8f);

            var resolved = rig.ResolveCameraPosition(lookTarget, desiredPosition);

            AssertVectorApproximately(desiredPosition, resolved);
        }
        finally
        {
            Object.DestroyImmediate(rigObject);
        }
    }

    [Test]
    public void ResolveCameraPositionPullsCameraInFrontOfBlockingWall()
    {
        var rigObject = new GameObject("Camera Rig Obstacle");
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            wall.name = "Camera Blocking Wall";
            wall.transform.position = new Vector3(0f, 1.8f, -2.5f);
            wall.transform.localScale = new Vector3(4f, 4f, 0.5f);
            Physics.SyncTransforms();

            var rig = rigObject.AddComponent<ThirdPersonCameraRig>();
            rig.collisionRadius = 0.2f;
            rig.collisionBuffer = 0.1f;
            var lookTarget = new Vector3(0f, 1.35f, 0f);
            var desiredPosition = new Vector3(0f, 2.2f, -4.8f);

            var resolved = rig.ResolveCameraPosition(lookTarget, desiredPosition);

            Assert.Less(resolved.z, lookTarget.z);
            Assert.Greater(resolved.z, wall.transform.position.z);
            Assert.Less(Vector3.Distance(lookTarget, resolved), Vector3.Distance(lookTarget, desiredPosition));
        }
        finally
        {
            Object.DestroyImmediate(wall);
            Object.DestroyImmediate(rigObject);
        }
    }

    private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
    {
        Assert.LessOrEqual(Vector3.Distance(expected, actual), 0.001f);
    }
}
