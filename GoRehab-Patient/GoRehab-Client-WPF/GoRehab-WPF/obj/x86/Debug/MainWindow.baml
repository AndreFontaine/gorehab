   M S B A M L   `   `   `  ����^  ZMicrosoft.Samples.Kinect.WpfViewers, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null�^clr-namespace:Microsoft.Samples.Kinect.WpfViewers;assembly=Microsoft.Samples.Kinect.WpfViewers#Microsoft.Samples.Kinect.WpfViewers  5   	    SkeletalTracking!   SkeletalTracking.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   mmy^clr-namespace:Microsoft.Samples.Kinect.WpfViewers;assembly=Microsoft.Samples.Kinect.WpfViewers     5   	   -   5          � Title$  
MainWindow��5   	   $	��600��6   $	��800��6)    � WindowState$ 	Maximized=�5   "   .��5      �� -   5        $��
MainCanvas��5      $��0,0,0,0q�6   .+�5	   
   ;   5Microsoft.Samples.Kinect.WpfViewers.KinectColorViewer  $	��640��6   $	��480��6+    �� Left$ 2��6y   5   /Microsoft.Samples.Kinect.WpfViewers.ImageViewer   Kinect 68   ��  �� ElementName$ kinectSensorChooser1�� �� Path$ Kinect�6y   A� 5
   
   -   5        $��headE��5
   R   $	 200��6   
 �� Top$ 0��6$   
���/���$��20��6F   $��20��6_    J� Opacity$ 1Q�6j   
�������6v   A� 5   
   -   5        $��centerShoulderE��5   S   $	 200��6   $ 50��6$   
���/���$��10��6G   $��10��6j   $ 1Q�6u   
�������6�   A� 5   
   -   5        $��spineE��5   T   $	 200��6   $	 100��6$   
���/���$��10��6H   $��10��6b   $ 1Q�6m   
�������6y   A� 5   
   -   5        $��
centerHipE��5   T   $	 200��6   $	 150��6$   
���/���$��10��6H   $��10��6f   $ 1Q�6q   
�������6}   A� 5   
   -   5        $��rightShoulderE��5   @   $	 250��6   $ 50��6$   $��10��64   $��10��6W   
���/���$ 1Q�6u   
�������6�   A� 5   
   -   5        $��rightElbowE��5   S   $	 280��6   $ 50��6$   
���/���$��10��6G   $��10��6f   $ 1Q�6q   
�������6}   A� 5   
   -	   5        $��rightWristE��5   S   $	 325��6   $ 50��6$   
���/���$��10��6G   $��10��6f   $ 1Q�6q   
�������6}   A� 5   
   -
   5        $��
rightHandE��5   S   $	 350��6   $ 50��6$   
���/���$��10��6G   $��10��6e   $ 1Q�6p   
�������6|   A� 5   
   -   5        $��	rightHipE��5   A   $	 250��6   $	 160��6$   $��10��65   $��10��6S   
���/���$ 1Q�6q   
�������6}   A� 5   
   -   5        $��
rightKneeE��5   T   $	 280��6   $	 170��6$   
���/���$��10��6H   $��10��6f   $ 1Q�6q   
�������6}   A� 5   
   -   5        $��rightAnkleE��5   T   $	 325��6   $	 180��6$   
���/���$��10��6H   $��10��6g   $ 1Q�6r   
�������6~   A� 5   
   -   5        $��
rightFootE��5   T   $	 350��6   $	 190��6$   
���/���$��10��6H   $��10��6f   $ 1Q�6q   
�������6}   A� 5   
   -   5        $��leftShoulderE��5   S   $	 150��6   $ 50��6$   
���/���$��10��6G   $��10��6h   $ 1Q�6s   
�������6   A� 5   
   -   5        $��
leftElbowE��5   S   $	 120��6   $ 50��6$   
���/���$��10��6G   $��10��6e   $ 1Q�6p   
�������6|   A� 5   
   -   5        $��
leftWristE��5   R   $ 75��6   $ 50��6#   
���/���$��10��6F   $��10��6d   $ 1Q�6o   
�������6{   A� 5   
   -   5        $��	leftHandE��5   R   $ 50��6   $ 50��6#   
���/���$��10��6F   $��10��6c   $ 1Q�6n   
�������6z   A� 5   
   -   5        $��leftHipE��5   T   $	 150��6   $	 160��6$   
���/���$��10��6H   $��10��6d   $ 1Q�6o   
�������6{   A� 5   
   -   5        $��	leftKneeE��5   T   $	 120��6   $	 170��6$   
���/���$��10��6H   $��10��6e   $ 1Q�6p   
�������6|   A� 5   
   -   5        $��
leftAnkleE��5   S   $ 75��6   $	 180��6#   
���/���$��10��6G   $��10��6e   $ 1Q�6p   
�������6|   A� 5   
   -   5        $��	leftFootE��5   S   $ 50��6   $	 190��6#   
���/���$��10��6G   $��10��6d   $ 1Q�6o   
�������6{   A� 5!   
   -   5        $��
SegmentRSS��5!   K   $	 385��6   $ 10��6$   
���  ��$��10��6?   $��10��6]   $ 1Q�6h   
�������6t   A� 5"   
   -   5        $��
SegmentLSS��5"   K   $	 350��6   $ 10��6$   
���  ��$��10��6?   $��10��6]   $ 1Q�6h   
�������6t   A� 5#   
   -   5        $��
SegmentRTS��5#   K   $	 325��6   $ 10��6$   
���  ��$��10��6?   $��10��6]   $ 1Q�6h   
�������6t   A� 5$   
   -   5        $��
SegmentLTS��5$   K   $	 290��6   $ 10��6$   
���  ��$��10��6?   $��10��6]   $ 1Q�6h   
�������6t   A� 5%   
   -   5        $��
SegmentRHS��5%   K   $	 325��6   $ 10��6$   
���  ��$��10��6?   $��10��6]   $ 1Q�6h   
�������6t   A� 5&   
   -   5        $��
SegmentLHS��5&   K   $	 290��6   $ 10��6$   
���  ��$��10��6?   $��10��6]   $ 1Q�6h   
�������6t   A� 5'   
   -   5        $��
SegmentRUS��5'   K   $	 325��6   $ 10��6$   
���  ��$��10��6?   $��10��6]   $ 1Q�6h   
�������6t   A� 5(   
   -   5        $��
SegmentLUS��5(   K   $	 290��6   $ 10��6$   
���  ��$��10��6?   $��10��6]   $ 1Q�6h   
�������6t   A� 5)   
   -   5        $��
SegmentHTS��5)   K   $	 290��6   $ 10��6$   
���  ��$��10��6?   $��10��6]   $ 1Q�6h   
�������6t   A� 5+   
   -    5        $��	SegmentCM��5+   N   $	 290��6   $ 10��6$   
��� ���$��20��6B   $��20��6_   $ 1Q�6j   
�������6v   =   7Microsoft.Samples.Kinect.WpfViewers.KinectSensorChooser  5-   
   -!   5        $��kinectSensorChooser1��5-   D   $	 250��6!   $	 380��63   $	��328��6`   �� 5.   
   $��Centro de Gravedad��6   $	 647��6-   $ 38��6?   �� 5/   
   -"   5        $��lblXCenterShoulder��5/   ?   $��X:��6   $	 647��6   $ 75��6/    J� RenderTransformOrigin$ 0.506,2.077&�6Y   �� 50   
   -#   5        $��textXCenterShoulder��50   T   $�� ��6   $��32��6   $	 672��6'   $ 69��69   $��62��6I   6T   �� 51   
   -$   5        $��lblYCenterShoulder��51   @   $��Y:��6   $	 648��6   $	 105��6/   6@   �� 52   
   -%   5        $��textYCenterShoulder��52   U   $�� ��6   $��30��6   $	 671��6'   $	 101��69   $��63��6J   6U   �� 53   
   -&   5        $��lblZCenterShoulder��53   @   $��Z:��6   $	 647��6   $	 134��6/   6@   �� 54   
   -'   5        $��textZCenterShoulder��54   U   $�� ��6   $��29��6   $	 671��6'   $	 131��69   $��63��6J   6U   �� 55   
   $��Punto Norte��6   $	 647��6&   $	 158��68   �� 56   
   -(   5        $��lblXCenterHip��56   @   $��X:��6   $	 647��6   $	 194��6/   $ 0.506,2.077&�6U   �� 57   
   -)   5        	 ��Name$	 textXCenterHip��57   U   $�� ��6   $��25��6   $	 672��6'   $	 192��69   $��62��6J   6U   �� 58   
   -*   5        $��lblYCenterHip��58   @   $��Y:��6   $	 647��6   $	 225��6/   6@   �� 59   
   -+   5        $	 textYCenterHip��59   U   $�� ��6   $��24��6   $	 673��6'   $	 227��69   $��62��6J   6U   �� 5:   
   -,   5        $��lblZCenterHip��5:   @   $��Z:��6   $	 647��6   $	 257��6/   6@   �� 5;   
   --   5        $	 textZCenterHip��5;   U   $�� ��6   $��28��6   $	 673��6'   $	 255��69   $��62��6J   6U   �� 5<   
   $��	Balance
��6   $	 647��6,   $	 304��6>   $��25��6O   $��58��6[   �� 5=   
   $��Estado:��6   $	 648��6"   $	 334��64   $��57��6E   �� 5>   
   -.   5        $	 	lblEstado��5>   o   $�� ��6   $	 710��6   $	 334��6-   $ 0.211,-0.038&�6>   $	��129��6c   6o   5B      5C      