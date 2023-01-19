using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Thrift.Protocol;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace MMIUnity.TargetEngine.Scene
{
    [Serializable]
    public struct JointTypeTransformPair
    {
        public MJointType type;
        public Transform t;
    }

    [ExecuteInEditMode]
    public class UnityHandPoseSimple : MonoBehaviour
    {

        public HandType HandType = HandType.Right;

        public Transform Wrist;
        public JointTypeTransformPair[] FingerTipTransforms;

        public void Start()
        {
            
        }

        public MConstraint GetReachConstraint(string id = "", string parentID = "")
        {
            if (id == "")
            {
                id = Guid.NewGuid().ToString();
            }

            MConstraint mc = new MConstraint(id)
            {
                GeometryConstraint = new MGeometryConstraint()
                {
                    ParentObjectID = parentID,
                    ParentToConstraint = new MTransform()
                    {
                        ID = Guid.NewGuid().ToString(),
                        Position = this.Wrist.position.ToMVector3(),
                        Rotation = this.Wrist.rotation.ToMQuaternion()
                    }
                }
            };
            return mc;
        }

        public MConstraint GetGraspConstraint(bool wristLocal, string id = "")
        {
            MPostureConstraint postureConstraint = new MPostureConstraint()
            {
                JointConstraints = new List<MJointConstraint>(),
                Posture = new MAvatarPostureValues("", new List<double>())
            };

            MJointType wrist = MJointType.LeftWrist;
            if (HandType == HandType.Right)
            {
                wrist = MJointType.RightWrist;
            }


            postureConstraint.JointConstraints.Add(_jointConstraint(wrist, this.Wrist));
            foreach (JointTypeTransformPair tp in this.FingerTipTransforms)
            {
                postureConstraint.JointConstraints.Add(_jointConstraint(tp.type, tp.t, wristLocal));
            }

            if (id == "")
            {
                id = Guid.NewGuid().ToString();
            }
            MConstraint mc = new MConstraint(id)
            {
                PostureConstraint = postureConstraint
            };
            return mc;
        }

        private MJointConstraint _jointConstraint(MJointType type, Transform t, bool wristLocal = false)
        {
            MJointConstraint c = new MJointConstraint(type)
            {
                GeometryConstraint = new MGeometryConstraint()
                {
                    ParentObjectID = "",
                    ParentToConstraint = new MTransform()
                    {
                        ID = Guid.NewGuid().ToString(),
                        Position = t.position.ToMVector3(),
                        Rotation = t.rotation.ToMQuaternion(),
                        Scale = new MVector3(1, 1, 1)
                    }
                }
            };
            return c;
        }

        public void Reset()
        {
            this.Wrist = _findForName(this.transform, "Wrist");
            if(this.Wrist != null)
            {
                if(this.Wrist.name.Contains("Left")) { this.HandType = HandType.Left; }
                else { this.HandType = HandType.Right; }
            }
            Transform thumb = this._findForName(this.transform, "ThumbTip");
            Transform index = this._findForName(this.transform, "IndexTip");
            Transform middle = this._findForName(this.transform, "MiddleTip");
            Transform ring = this._findForName(this.transform, "RingTip");
            Transform pinky = this._findForName(this.transform, "LittleTip");
            List<JointTypeTransformPair> tmp = new List<JointTypeTransformPair>(); 
            if (this.HandType == HandType.Left) {
                if (thumb != null) { tmp.Add(new JointTypeTransformPair() { t = thumb, type = MJointType.LeftThumbTip }); }
                if (index != null) { tmp.Add(new JointTypeTransformPair() { t = index, type = MJointType.LeftIndexTip }); }
                if (middle != null) { tmp.Add(new JointTypeTransformPair() { t = middle, type = MJointType.LeftMiddleTip }); }
                if (ring != null) { tmp.Add(new JointTypeTransformPair() { t = ring, type = MJointType.LeftRingTip }); }
                if (pinky != null) { tmp.Add(new JointTypeTransformPair() { t = pinky, type = MJointType.LeftLittleTip }); }
            }
            else
            {
                if (thumb != null) { tmp.Add(new JointTypeTransformPair() { t = thumb, type = MJointType.RightThumbTip }); }
                if (index != null) { tmp.Add(new JointTypeTransformPair() { t = index, type = MJointType.RightIndexTip }); }
                if (middle != null) { tmp.Add(new JointTypeTransformPair() { t = middle, type = MJointType.RightMiddleTip }); }
                if (ring != null) { tmp.Add(new JointTypeTransformPair() { t = ring, type = MJointType.RightRingTip }); }
                if (pinky != null) { tmp.Add(new JointTypeTransformPair() { t = pinky, type = MJointType.RightIndexTip }); }
            }
            this.FingerTipTransforms = tmp.ToArray();
            // guess default variable.    
        }

        private Transform _findForName(Transform t, string partialName)
        {
            if(t.name.Contains(partialName)) { return t; }
            for (int i = 0; i < t.childCount; i++)
            {
                Transform r = _findForName(t.GetChild(i), partialName);
                if (r != null)
                {
                    return r;
                }
            }
            return null;
        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UnityHandPoseSimple))]
    public class UnityHandPoseSimpleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.HelpBox("Helper Functions", MessageType.Info);

            UnityHandPoseSimple script = (UnityHandPoseSimple)target;
            if(GUILayout.Button("Print Grasp Constraint")) {
                Debug.Log(PrintExtensions.ToString(script.GetGraspConstraint(false)));
            }
        }
    }

#endif
    public static class PrintExtensions
    {

        public static string ToString(this MConstraint c)
        {
            string s = "";
            s += $"ID: {c.ID}";
            if (c.GeometryConstraint != null) { s += $"GeometryConstraint: ({c.GeometryConstraint.ToString()}), "; }
            if (c.PostureConstraint != null) { s += $"PostureConstraint: ({ToString(c.PostureConstraint)}), "; }
            return s;
        }

        public static string ToString(MPostureConstraint c)
        {
            string s = "";
            s += $"MConstraint( Posture: {c.Posture}, JointConstraints: [";
            foreach(var jc in c.JointConstraints)
            {
                s += $"{jc}, ";
            }
            s += "]";
            return s;
        }
        /*
        public static string ToString(this MJointConstraint m)
        {
            return $"Type: {m.JointType}, GeometryConstraint: {m.GeometryConstraint}";
        }*/
    }
}

