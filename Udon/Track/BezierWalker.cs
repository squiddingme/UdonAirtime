
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Airtime.Track
{
    public class BezierWalker : UdonSharpBehaviour
    {
        public BezierTrack track;

        // Position
        public bool walkByDistance = false;
        public float trackPosition = 0.0f;

        // Direction
        public const float FORWARD = 1.0f;
        public const float BACKWARD = -1.0f;
        public float trackDirection = FORWARD;

        public void SetTrack(BezierTrack value)
        {
            track = value;
            trackPosition = 0.0f;
            trackDirection = FORWARD;
        }

        public Vector3 GetPoint()
        {
            return walkByDistance ? track.GetPointByDistance(trackPosition) : track.GetPoint(trackPosition);
        }

        public Vector3 GetPointByTime()
        {
            return track.GetPoint(trackPosition);
        }

        public Vector3 GetPointByDistance()
        {
            return track.GetPointByDistance(trackPosition);
        }

        public float GetConstantSpeed(float distance)
        {
            // compute the next track position as a constant speed by calculating it using the actual distance given
            float arcNextTrackPosition = trackPosition + (trackDirection * distance * Time.deltaTime);
            float multiplier = (distance * Time.deltaTime) / Vector3.Distance(track.GetPointByDistance(trackPosition), track.GetPointByDistance(arcNextTrackPosition));
            
            return distance * multiplier;
        }

        public Vector3 GetPointAfterTime(float time)
        {
            trackPosition += (trackDirection * time) * Time.deltaTime;

            if (track.GetIsLoop())
            {
                if (trackPosition >= 1.0f)
                {
                    trackPosition -= 1.0f;
                }
                else if (trackPosition <= 0.0f)
                {
                    trackPosition += 1.0f;
                }
            }

            return track.GetPoint(trackPosition);
        }

        public Vector3 GetPointAfterDistance(float distance)
        {
            trackPosition += (trackDirection * GetConstantSpeed(distance)) * Time.deltaTime;

            if (track.GetIsLoop())
            {
                if (trackPosition >= track.cachedDistance)
                {
                    trackPosition -= track.cachedDistance;
                }
                else if (trackPosition <= 0.0f)
                {
                    trackPosition += track.cachedDistance;
                }
            }

            return track.GetPointByDistance(trackPosition);
        }

        public bool GetIsDone()
        {
            if (track.GetIsLoop())
            {
                // the ride never ends
                return false;
            }
            else
            {
                if (walkByDistance)
                {
                    return (trackPosition <= 0.0f || trackPosition >= track.cachedDistance);
                }
                else
                {
                    return (trackPosition <= 0.0f || trackPosition >= 1.0f);
                }
            }
        }
    }
}