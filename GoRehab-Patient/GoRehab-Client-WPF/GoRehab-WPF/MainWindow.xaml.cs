// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Coding4Fun.Kinect.Wpf;
using Microsoft.Kinect;

namespace SkeletalTracking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        bool closing = false;
        const int skeletonCount = 6; 
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];

        //Constantes Antropomorficas de la distancia desde el punto proximal del segmento
        
        const float comShank = 0.433F;
        const float comThigh = 0.433F;
        const float comHandFor = 0.682F;
        const float comUpArm = 0.436F;
        const float comHeadTr = 0.540F;

        //Constantes Antropomorficas de la fracción de masa corporal de cada segmento

        const float bmShank = 0.044F;
        const float bmThigh = 0.015F;
        const float bmHandFor = 0.025F;
        const float bmUpArm = 0.031F;
        const float bmHeadTr = 0.532F;


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);

        }

        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor old = (KinectSensor)e.OldValue;

            StopKinect(old);

            KinectSensor sensor = (KinectSensor)e.NewValue;

            if (sensor == null)
            {
                return;
            }

            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.0f,
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 1.0f,
                MaxDeviationRadius = 0.1f
            };
            
            sensor.SkeletonStream.Enable(parameters);

            sensor.SkeletonStream.Enable();

            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30); 
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            
            try
            {
                sensor.Start();
            }
            
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }
        }

        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (closing)
            {
                return;
            }

            //Get a skeleton
            Skeleton first =  GetFirstSkeleton(e);

            if (first == null)
            {
                return; 
            }

            GetCameraPoint(first, e); 

        }

        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {

            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null || kinectSensorChooser1.Kinect == null)
                {
                    return;
                }
                

                //Map a joint location to a point on the depth map
                
                //head
                DepthImagePoint headDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.Head].Position);
                
                //shouler center
                SkeletonPoint centerShoulder = first.Joints[JointType.ShoulderCenter].Position;
                DepthImagePoint centerShoulderDepthPoint =
                    depth.MapFromSkeletonPoint(centerShoulder);

                //spine
                DepthImagePoint spineDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.Spine].Position);
                
                //hip center
                SkeletonPoint centerHip = first.Joints[JointType.HipCenter].Position;
                DepthImagePoint centerHipDepthPoint = depth.MapFromSkeletonPoint(centerHip);

                //left elbow
                DepthImagePoint leftElbowDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.ElbowLeft].Position);
                //left shoulder
                DepthImagePoint leftShoulderDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.ShoulderLeft].Position);
                //left wrist
                DepthImagePoint leftWristDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.WristLeft].Position);
                //left hand
                DepthImagePoint leftHandDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandLeft].Position);
                //left hip
                DepthImagePoint leftHipDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HipLeft].Position);
                //left knee    
                DepthImagePoint leftKneeDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.KneeLeft].Position);
                //left ankle
                DepthImagePoint leftAnkleDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.AnkleLeft].Position);
                //left foot
                DepthImagePoint leftFootDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.FootLeft].Position);

                //right elbow
                DepthImagePoint rightElbowDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.ElbowRight].Position);
                //right shoulder
                DepthImagePoint rightShoulderDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.ShoulderRight].Position);
                //right wrist
                DepthImagePoint rightWristDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.WristRight].Position);
                //right hand
                DepthImagePoint rightHandDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandRight].Position);
                //right hip
                DepthImagePoint rightHipDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HipRight].Position);
                //right knee
                DepthImagePoint rightKneeDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.KneeRight].Position);
                //right hand
                DepthImagePoint rightAnkleDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.AnkleRight].Position);
                //right foot
                DepthImagePoint rightFootDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.FootRight].Position);
                
                //Centros de masa de segmentos corporales
                
                /*Segmentos Shank (Pierna) : Rodilla a Tobillo */
                
                SkeletonPoint rightShankSegment = centroMasaSegmento(first.Joints[JointType.KneeRight].Position,
                    first.Joints[JointType.AnkleRight].Position, comShank);
                DepthImagePoint rightShankSegmentDepthPoint = depth.MapFromSkeletonPoint(rightShankSegment);
                
                SkeletonPoint leftShankSegment = centroMasaSegmento(first.Joints[JointType.KneeLeft].Position,
                    first.Joints[JointType.AnkleLeft].Position, comShank);
                DepthImagePoint leftShankSegmentDepthPoint = depth.MapFromSkeletonPoint(leftShankSegment);

                /*Segmentos Thigh (Muslo) : Cadera a Rodilla*/

                SkeletonPoint rightThighSegment = centroMasaSegmento(first.Joints[JointType.HipCenter].Position,
                    first.Joints[JointType.KneeRight].Position, comThigh);
                DepthImagePoint rightThighSegmentDepthPoint = depth.MapFromSkeletonPoint(rightThighSegment);

                SkeletonPoint leftThighSegment = centroMasaSegmento(first.Joints[JointType.HipCenter].Position,
                    first.Joints[JointType.KneeLeft].Position, comThigh);
                DepthImagePoint leftThighSegmentDepthPoint = depth.MapFromSkeletonPoint(leftThighSegment);

                /*Segmentos Hand and Forearm (Brazo y antebrazo) : Codo a Muñeca*/

                SkeletonPoint rightHandForSegment = centroMasaSegmento(first.Joints[JointType.ElbowRight].Position,
                    first.Joints[JointType.WristRight].Position, comHandFor);
                DepthImagePoint rightHandForSegmentDepthPoint = depth.MapFromSkeletonPoint(rightHandForSegment);

                SkeletonPoint leftHandForSegment = centroMasaSegmento(first.Joints[JointType.ElbowLeft].Position,
                    first.Joints[JointType.WristLeft].Position, comHandFor);
                DepthImagePoint leftHandForSegmentDepthPoint = depth.MapFromSkeletonPoint(leftHandForSegment);

                /*Segmentos Upper Arm (Parte superior del barzo) : Hombro a Codo*/

                SkeletonPoint rightUpperArmSegment = centroMasaSegmento(first.Joints[JointType.ShoulderRight].Position,
                    first.Joints[JointType.ElbowRight].Position, comUpArm);
                DepthImagePoint rightUpperArmSegmentDepthPoint = depth.MapFromSkeletonPoint(rightUpperArmSegment);

                SkeletonPoint leftUpperArmSegment = centroMasaSegmento(first.Joints[JointType.ShoulderLeft].Position,
                    first.Joints[JointType.ElbowLeft].Position, comUpArm);
                DepthImagePoint leftUpperArmSegmentDepthPoint = depth.MapFromSkeletonPoint(leftUpperArmSegment);

                /*Segmento Head and Trunk (Cabeza y tronco) : Centro de cadera a Centro de hombros*/

                SkeletonPoint HeadTrSegment = centroMasaSegmento(first.Joints[JointType.HipCenter].Position,
                    first.Joints[JointType.ShoulderCenter].Position, comHeadTr);
                DepthImagePoint HeadTrSegmentDepthPoint = depth.MapFromSkeletonPoint(HeadTrSegment);

                /*Centro de masa corporal*/

                SkeletonPoint centerMass = centroMasa(rightShankSegment, leftShankSegment, rightThighSegment,
                    leftThighSegment, rightHandForSegment, leftHandForSegment, rightUpperArmSegment, leftUpperArmSegment,
                    HeadTrSegment);
                DepthImagePoint centerMassDepthPoint = depth.MapFromSkeletonPoint(centerMass);

                //DepthImagePoint centerShoulderDepthPoint = depth.MapFromSkeletonPoint(centerShoulder);
                
                //Map a depth point to a point on the color image
                
                //head
                ColorImagePoint headColorPoint =
                    depth.MapToColorImagePoint(headDepthPoint.X, headDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //center shoulder
                ColorImagePoint centerShoulderColorPoint =
                    depth.MapToColorImagePoint(centerShoulderDepthPoint.X, centerShoulderDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //spine
                ColorImagePoint spineColorPoint =
                    depth.MapToColorImagePoint(spineDepthPoint.X, spineDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //center hip
                ColorImagePoint centerHipColorPoint =
                    depth.MapToColorImagePoint(centerHipDepthPoint.X, centerHipDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //left shoulder
                ColorImagePoint leftShoulderColorPoint =
                    depth.MapToColorImagePoint(leftShoulderDepthPoint.X, leftShoulderDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //left wrist
                ColorImagePoint leftElbowColorPoint =
                    depth.MapToColorImagePoint(leftElbowDepthPoint.X, leftElbowDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //left hand
                ColorImagePoint leftWristColorPoint =
                    depth.MapToColorImagePoint(leftWristDepthPoint.X, leftWristDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //left wrist
                ColorImagePoint leftHandColorPoint =
                    depth.MapToColorImagePoint(leftHandDepthPoint.X, leftHandDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //left hip
                ColorImagePoint leftHipColorPoint =
                    depth.MapToColorImagePoint(leftHipDepthPoint.X, leftHipDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //left knee
                ColorImagePoint leftKneeColorPoint =
                    depth.MapToColorImagePoint(leftKneeDepthPoint.X, leftKneeDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //left ankle
                ColorImagePoint leftAnkleColorPoint =
                    depth.MapToColorImagePoint(leftAnkleDepthPoint.X, leftAnkleDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //left foot
                ColorImagePoint leftFootColorPoint =
                    depth.MapToColorImagePoint(leftFootDepthPoint.X, leftFootDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                   
                //right shoulder
                ColorImagePoint rightShoulderColorPoint =
                    depth.MapToColorImagePoint(rightShoulderDepthPoint.X, rightShoulderDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right elbow
                ColorImagePoint rightElbowColorPoint =
                    depth.MapToColorImagePoint(rightElbowDepthPoint.X, rightElbowDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right wrist
                ColorImagePoint rightWristColorPoint =
                    depth.MapToColorImagePoint(rightWristDepthPoint.X, rightWristDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right hand
                ColorImagePoint rightHandColorPoint =
                    depth.MapToColorImagePoint(rightHandDepthPoint.X, rightHandDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right hip
                ColorImagePoint rightHipColorPoint =
                    depth.MapToColorImagePoint(rightHipDepthPoint.X, rightHipDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right knee
                ColorImagePoint rightKneeColorPoint =
                    depth.MapToColorImagePoint(rightKneeDepthPoint.X, rightKneeDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right ankle
                ColorImagePoint rightAnkleColorPoint =
                    depth.MapToColorImagePoint(rightAnkleDepthPoint.X, rightAnkleDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right foot
                ColorImagePoint rightFootColorPoint =
                    depth.MapToColorImagePoint(rightFootDepthPoint.X, rightFootDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //Right Shank Segment
                ColorImagePoint rightShankSegmentColorPoint =
                    depth.MapToColorImagePoint(rightShankSegmentDepthPoint.X, rightShankSegmentDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //Left Shank Segment
                ColorImagePoint leftShankSegmentColorPoint =
                    depth.MapToColorImagePoint(leftShankSegmentDepthPoint.X, leftShankSegmentDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //Right Thigh Segment
                ColorImagePoint rightThighSegmentColorPoint =
                    depth.MapToColorImagePoint(rightThighSegmentDepthPoint.X, rightThighSegmentDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //Left Thigh Segment
                ColorImagePoint leftThighSegmentColorPoint =
                    depth.MapToColorImagePoint(leftThighSegmentDepthPoint.X, leftThighSegmentDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //Right HandFor Segment
                ColorImagePoint rightHandForSegmentColorPoint =
                    depth.MapToColorImagePoint(rightHandForSegmentDepthPoint.X, rightHandForSegmentDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //Left HandFor Segment
                ColorImagePoint leftHandForSegmentColorPoint =
                    depth.MapToColorImagePoint(leftHandForSegmentDepthPoint.X, leftHandForSegmentDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //Right UpperArm Segment
                ColorImagePoint rightUpperArmSegmentColorPoint =
                    depth.MapToColorImagePoint(rightUpperArmSegmentDepthPoint.X, rightUpperArmSegmentDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //Left UpperArm Segment
                ColorImagePoint leftUpperArmSegmentColorPoint =
                    depth.MapToColorImagePoint(leftUpperArmSegmentDepthPoint.X, leftUpperArmSegmentDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //Head and Trunk Segment
                ColorImagePoint HeadTrSegmentColorPoint =
                    depth.MapToColorImagePoint(HeadTrSegmentDepthPoint.X, HeadTrSegmentDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //Center of the Mass
                ColorImagePoint centerMassColorPoint =
                    depth.MapToColorImagePoint(centerMassDepthPoint.X, centerMassDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                
                

                //Set location
                CameraPosition(headE, headColorPoint);
                CameraPosition(centerShoulderE, centerShoulderColorPoint);
                CameraPosition(centerHipE, centerHipColorPoint);
                CameraPosition(spineE, spineColorPoint);

                CameraPosition(rightShoulderE, rightShoulderColorPoint);
                CameraPosition(rightElbowE, rightElbowColorPoint);
                CameraPosition(rightWristE, rightWristColorPoint);
                CameraPosition(rightHandE, rightHandColorPoint);
                CameraPosition(rightHipE, rightHipColorPoint);
                CameraPosition(rightKneeE, rightKneeColorPoint);
                CameraPosition(rightAnkleE, rightAnkleColorPoint);
                CameraPosition(rightFootE, rightFootColorPoint);

                CameraPosition(leftShoulderE, leftShoulderColorPoint);
                CameraPosition(leftElbowE, leftElbowColorPoint);
                CameraPosition(leftWristE, leftWristColorPoint);
                CameraPosition(leftHandE, leftHandColorPoint);
                CameraPosition(leftHipE, leftHipColorPoint);
                CameraPosition(leftKneeE, leftKneeColorPoint);
                CameraPosition(leftAnkleE, leftAnkleColorPoint);
                CameraPosition(leftFootE, leftFootColorPoint);

                CameraPosition(SegmentRSS, rightShankSegmentColorPoint);
                CameraPosition(SegmentLSS, leftShankSegmentColorPoint);
                CameraPosition(SegmentRTS, rightThighSegmentColorPoint);
                CameraPosition(SegmentLTS, leftThighSegmentColorPoint);
                CameraPosition(SegmentRHS, rightHandForSegmentColorPoint);
                CameraPosition(SegmentLHS, leftHandForSegmentColorPoint);
                CameraPosition(SegmentRUS, rightUpperArmSegmentColorPoint);
                CameraPosition(SegmentLUS, leftUpperArmSegmentColorPoint);
                CameraPosition(SegmentHTS, HeadTrSegmentColorPoint);

                CameraPosition(SegmentCM, centerMassColorPoint);

                //Asignacion Valores X, Y, Z a puntos para determinar balance
                //Centro de los Hombros - CenterShoulder
                textXCenterShoulder.Content = centerShoulder.X;
                textYCenterShoulder.Content = centerShoulder.Y;
                textZCenterShoulder.Content = centerShoulder.Z;
                
                //Centro de cadera - CenterHip
                textXCenterHip.Content = centerHip.X;
                textYCenterHip.Content = centerHip.Y;
                textZCenterHip.Content = centerHip.Z;

                //Balance
                balance(centerHip, centerShoulder);
                
            }        
        }


        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null; 
                }

                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                         where s.TrackingState == SkeletonTrackingState.Tracked
                                         select s).FirstOrDefault();

                return first;

            }
        }

        private void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    //stop sensor 
                    sensor.Stop();

                    //stop audio if not null
                    if (sensor.AudioSource != null)
                    {
                        sensor.AudioSource.Stop();
                    }


                }
            }
        }

        private void CameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X - element.Width / 2);
            Canvas.SetTop(element, point.Y - element.Height / 2);

        }

        private void ScalePosition(FrameworkElement element, Joint joint)
        {
            //convert the value to X/Y
            //Joint scaledJoint = joint.ScaleTo(1280, 720); 
            
            //convert & scale (.3 = means 1/3 of joint distance)
            Joint scaledJoint = joint.ScaleTo(1280, 720, .3f, .3f);

            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y); 
            
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true; 
            StopKinect(kinectSensorChooser1.Kinect); 
        }

        private SkeletonPoint centroMasaSegmento (SkeletonPoint ProximalEndPoint, SkeletonPoint DistalEndPoint, float COM)
        {
            SkeletonPoint reference = ProximalEndPoint;
            reference.X = ProximalEndPoint.X + ((DistalEndPoint.X - ProximalEndPoint.X) * COM);
            reference.Y = ProximalEndPoint.Y + ((DistalEndPoint.Y - ProximalEndPoint.Y) * COM);
            reference.Z = ProximalEndPoint.Z + ((DistalEndPoint.Z - ProximalEndPoint.Z) * COM);
            return reference;
        }

        private SkeletonPoint centroMasa(SkeletonPoint rightShank, SkeletonPoint leftShank, SkeletonPoint rightThigh,
            SkeletonPoint leftThigh, SkeletonPoint rightHandFor, SkeletonPoint leftHandFor, SkeletonPoint rightUppArm,
            SkeletonPoint leftUppArm, SkeletonPoint headTr)
        {
            SkeletonPoint reference = leftShank;
            
            float nomX, demX, nomY, demY, nomZ, demZ;
            
            nomX = compMasaX(leftShank, bmShank) + compMasaX(rightShank, bmShank) + compMasaX(leftThigh, bmThigh) +
            compMasaX(rightThigh, bmThigh) + compMasaX(leftHandFor, bmHandFor) + compMasaX(rightHandFor, bmHandFor) +
            compMasaX(leftUppArm, bmUpArm) + compMasaX(rightUppArm, bmUpArm) + compMasaX(headTr, bmHeadTr);

            demX = leftShank.X + leftThigh.X + leftHandFor.X + leftUppArm.X + leftUppArm.X + headTr.X + rightShank.X 
                + rightThigh.X + rightHandFor.X + rightUppArm.X;

            nomY = compMasaY(leftShank, bmShank) + compMasaY(rightShank, bmShank) + compMasaY(leftThigh, bmThigh) +
            compMasaY(rightThigh, bmThigh) + compMasaY(leftHandFor, bmHandFor) + compMasaY(rightHandFor, bmHandFor) +
            compMasaY(leftUppArm, bmUpArm) + compMasaY(rightUppArm, bmUpArm) + compMasaY(headTr, bmHeadTr);

            demY = leftShank.Y + leftThigh.Y + leftHandFor.Y + leftUppArm.Y + leftUppArm.Y + headTr.Y + rightShank.Y
                + rightThigh.Y + rightHandFor.Y + rightUppArm.Y;

            nomZ = compMasaZ(leftShank, bmShank) + compMasaZ(rightShank, bmShank) + compMasaZ(leftThigh, bmThigh) +
            compMasaZ(rightThigh, bmThigh) + compMasaZ(leftHandFor, bmHandFor) + compMasaZ(rightHandFor, bmHandFor) +
            compMasaZ(leftUppArm, bmUpArm) + compMasaZ(rightUppArm, bmUpArm) + compMasaZ(headTr, bmHeadTr);

            demZ = leftShank.Z + leftThigh.Z + leftHandFor.Z + leftUppArm.Z + leftUppArm.Z + headTr.X + rightShank.Z
                + rightThigh.Z + rightHandFor.Z + rightUppArm.Z;
            
            reference.X = nomX / demX;
            reference.Y = nomY / demY;
            reference.Z = nomZ / demZ;
            
            return reference;
        }

        //Componente X de un segmento para su centro de masa 
        
        private float compMasaX(SkeletonPoint actualPoint, float BM)
        {
            SkeletonPoint reference = actualPoint;
            reference.X = actualPoint.X * BM;
            return reference.X;
        }

        //Componente Y de un segmento para su centro de masa 

        private float compMasaY(SkeletonPoint actualPoint, float BM)
        {
            SkeletonPoint reference = actualPoint;
            reference.Y = actualPoint.Y * BM;
            return reference.Y;
        }

        //Componente Z de un segmento para su centro de masa 

        private float compMasaZ(SkeletonPoint actualPoint, float BM)
        {
            SkeletonPoint reference = actualPoint;
            reference.Z = actualPoint.Z * BM;
            return reference.Z;
        }

        private void balance(SkeletonPoint hipCenter, SkeletonPoint shoulderCenter)
        {

            bool adelante = hipCenter.Z > shoulderCenter.Z + 0.1;
            bool atras = hipCenter.Z < shoulderCenter.Z - 0.1;
            bool derecha = hipCenter.X > shoulderCenter.X + 0.1;
            bool izquierda = hipCenter.X < shoulderCenter.X - 0.1;

            if (adelante)
            {
                if (derecha)
                {
                    lblEstado.Content = "adelante - izquierda";
                }
                else if (izquierda)
                {
                    lblEstado.Content = "adelante - derecha";
                }
                else
                {
                    lblEstado.Content = "adelante";
                }
            }
            else if (atras)
            {
                if (derecha)
                {
                    lblEstado.Content = "atras - izquierda";
                }
                else if (izquierda)
                {
                    lblEstado.Content = "atras - derecha";
                }
                else
                {
                    lblEstado.Content = "atras";
                }
            }
            else
            {
                if (derecha)
                {
                    lblEstado.Content = "izquierda";
                }
                else if (izquierda)
                {
                    lblEstado.Content = "derecha";
                }
                else
                {
                    lblEstado.Content = "OK";
                }
            }

        }

    }
}
