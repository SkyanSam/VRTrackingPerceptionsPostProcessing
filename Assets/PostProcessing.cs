using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class PostProcessing : MonoBehaviour
{
    public long currentTick;
    string vrcString = "";
    string oscString = "";
    List<Vector3> VRCpositions = new List<Vector3>();
    List<Vector3> VRCrotations = new List<Vector3>();
    public List<long> VRCticks = new List<long>();
    public long firstTick;
    public long lastTick;
    public long sceneStartTick;
    public string systemUser = "umdxr";
    public LineRenderer debugLine;

    private List<Vector2> OSCgazes = new List<Vector2>();
    public List<long> OSCticks = new List<long>();

    public float raycastMaxDistance;
    void Start()
    {
        // VRC
        
        string folderPath = $@"C:\Users\{systemUser}\AppData\LocalLow\VRChat\VRChat";
        string[] logFiles = Directory.GetFiles(folderPath, "*.txt");

        if (logFiles.Length > 0)
        {
            // Sort files by creation time and get the latest one
            FileInfo latestLogFile = new DirectoryInfo(folderPath)
                .GetFiles("*.txt")
                .OrderByDescending(f => f.CreationTime)
                .FirstOrDefault();

            if (latestLogFile != null)
            {
                // Read the contents of the latest log file
                Debug.Log(latestLogFile.FullName);
                vrcString = File.ReadAllText(latestLogFile.FullName);
                Debug.Log(vrcString);
            }
            else
            {
                Debug.Log("No log files found in the folder.");
            }
        }
        else
        {
            Debug.Log("No log files found in the folder.");
        }
        

        vrcString = vrcString.Replace(":", ",");
        var split = vrcString.Split(' ', '\n');
        foreach (var clause in split)
        {
            if (clause.StartsWith("VTRP"))
            {
                var split2 = clause.Split('|');
                foreach (var s in split2)
                {
                    if (s.StartsWith("position") || s.StartsWith("rotation"))
                    {
                        var split3 = s.Split(",");
                            
                        var vec = (new Vector3(float.Parse(split3[1]), float.Parse(split3[2]), float.Parse(split3[3])));
                        if (s.StartsWith("position")) VRCpositions.Add(vec);
                        if (s.StartsWith("rotation")) VRCrotations.Add(vec);
                    }
                    if (s.StartsWith("ticks"))
                    {
                        var split3 = s.Split(",");
                        VRCticks.Add(long.Parse(split3[1]));
                    }
                }
            }
        }
        
        // OSC
        
        
        oscString = File.ReadAllText(@"C:\\ProgramData\\vrtp_log.txt");
        Debug.Log(oscString);
        var split4 = oscString.Split('\n');
        foreach (var clause in split4)
        {
            var split5 = clause.Split('|');
            foreach (var s in split5)
            {
                if (s.StartsWith("gaze"))
                {
                    var split3 = s.Split(",");
                    var vec = (new Vector2(float.Parse(split3[1]), float.Parse(split3[2])));
                    OSCgazes.Add(vec);
                }

                if (s.StartsWith("fAngleX"))
                {
                    var split3 = s.Split(",");
                    var vec = (new Vector2(float.Parse(split3[1]), 0f));
                    OSCgazes.Add(vec);
                }

                if (s.StartsWith("fAngleY"))
                {
                    var split3 = s.Split(",");
                    var v = OSCgazes[OSCgazes.Count - 1];
                    v.y = float.Parse(split3[1]);
                    OSCgazes[OSCgazes.Count - 1] = v;
                }
                if (s.StartsWith("ticks") || s.StartsWith("time"))
                {
                    var split3 = s.Split(",");
                    OSCticks.Add(long.Parse(split3[1]));
                }
            }
        }
        
        // Finding First/Last Tick
        firstTick = OSCticks[0] > VRCticks[0] ? OSCticks[0] : VRCticks[0];
        lastTick = OSCticks[OSCticks.Count - 1] < VRCticks[VRCticks.Count - 1] ? OSCticks[OSCticks.Count - 1] : VRCticks[VRCticks.Count - 1];

        sceneStartTick = DateTime.UtcNow.Ticks;
    }

    int FindIndex(long tick, ref List<long> ticks)
    {
        for (int i = 0; i < ticks.Count; i++)
        {
            if (tick <= ticks[i])
            {
                return i;
            }
            // if trick < ticks[i] it will keep continuing..
        }

        return -1;
    }
    
    // Update is called once per frame
    void Update()
    {
        currentTick = DateTime.UtcNow.Ticks - sceneStartTick + firstTick;
        if (currentTick <= lastTick)
        {
            int closestVRCtick = FindIndex(currentTick, ref VRCticks);
            int closestOSCtick = FindIndex(currentTick, ref OSCticks);
            Debug.Log($"{closestVRCtick}, {closestOSCtick}");
            transform.position = VRCpositions[closestVRCtick];
            transform.eulerAngles = VRCrotations[closestVRCtick];

            Quaternion gazeQ = new Quaternion();
            gazeQ.eulerAngles = new Vector3(OSCgazes[closestOSCtick][0] * 45, OSCgazes[closestOSCtick][1] * 45, 0);
            Vector3 gaze = gazeQ * Vector3.forward;
            
            RaycastHit hit;
            var isHit = Physics.Raycast(new Ray(transform.position, transform.rotation * gaze), out hit, raycastMaxDistance, LayerMask.GetMask("EyeTracking"));
            debugLine.SetPosition(0, transform.position);
            debugLine.SetPosition(1, transform.rotation * gaze * raycastMaxDistance);
            //Gizmos.DrawLine(transform.position, transform.rotation * gaze);
            if (isHit)
            {
                print("HIT SUCCESSFUL");
            }
        }
    }
}
