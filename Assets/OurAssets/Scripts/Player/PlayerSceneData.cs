using UnityEngine;

[System.Serializable]
public class SerializedPlayerSceneData
{
    public float[] Position;
    public float[] EulerAngles;
    public float[] CameraEulerAngles;

    public PlayerSceneData Deserialized
    {
        get
        {
            return new PlayerSceneData()
            {
                Position = new Vector3(Position[0], Position[1], Position[2]),
                EulerAngles = new Vector3(EulerAngles[0], EulerAngles[1], EulerAngles[2]),
                CameraEulerAngles = new Vector3(CameraEulerAngles[0], CameraEulerAngles[1], CameraEulerAngles[2])
            };
        }
    }
}

public class PlayerSceneData
{
    public Vector3 Position { get; set; }
    public Vector3 EulerAngles { get; set; }
    public Vector3 CameraEulerAngles { get; set; }

    public SerializedPlayerSceneData Serialized
    {
        get
        {
            return new SerializedPlayerSceneData()
            {
                Position = new float[3] { Position.x, Position.y, Position.z },
                EulerAngles = new float[3] { EulerAngles.x, EulerAngles.y, EulerAngles.z },
                CameraEulerAngles = new float[3] { CameraEulerAngles.x, CameraEulerAngles.y, CameraEulerAngles.z }
            };
        }
    }
}
