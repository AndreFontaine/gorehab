   M S B A M L   `   `   `  ����S  OMicrosoft.Kinect.Toolkit, Version=1.8.0.0, Culture=neutral, PublicKeyToken=nulleHclr-namespace:Microsoft.Kinect.Toolkit;assembly=Microsoft.Kinect.ToolkitMicrosoft.Kinect.Toolkit  5   	   N JWebserverBasics-WPF, Version=0.0.0.0, Culture=neutral, PublicKeyToken=nullc6clr-namespace:Microsoft.Samples.Kinect.WebserverBasics(Microsoft.Samples.Kinect.WebserverBasics 5   	   9   3Microsoft.Samples.Kinect.WebserverBasics.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   WtkHclr-namespace:Microsoft.Kinect.Toolkit;assembly=Microsoft.Kinect.Toolkit     5   	   Hlocal6clr-namespace:Microsoft.Samples.Kinect.WebserverBasics     5   	   -   5          � Title$  Webserver Basics��5   	    � SizeToContent$ Height=�6"   $	��800��69   $��16(�6E    � 	Resources 5	      F  @Microsoft.Samples.Kinect.WebserverBasics.InverseBooleanConverter   inverseBooleanConverter��%   &	          5
   
   6(   5      .��5      �  � ColumnDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $��* �6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   .+�5   
   4   .Microsoft.Kinect.Toolkit.KinectSensorChooserUI  $��0��6#   $��0��60   $
��Left=�6t      IsListening .  $��5q�6�      KinectSensorChooser 6@   �� *KinectSensorChooser+6�   ]� 5   
   $��0��6   $��1��6!   $��5q�61   .+�5      �� -   5        $	��Top=�5      $��50��6*   $	��100��66   $��Start��6B   {�5      ��  �� Path$ 	IsStarted� �� 	Converter# [  6n   �� 5      -   5        $	��Top=�5      $��50��6*   $	��100��66   $
��Stop��6B   {�5      �� $ 	IsStarted�6:   5      �� 5   
   $��0��6   $��2��6"   $
��10 0q�62   .+�5      �� -   5        	 ��Name$	 OpenBrowserText��5      $	��0 5q�61   �� 5       -   5        
 ��Name$
 UriLink��5    *   $��16(�6   .�6R   &$Open GoRehab page in default browser6x   �� 5!      �� 5"      $��11(�6   .�6$   20See documentation for list of supported browsers6V   5#      �� 5$      -   5        $	 ServingFilesText��5$      $	��0 5q�62   $��11(�6?   $��WrapWithOverflow=�6M   �� 5%      $��14(�6   $
��Bold"�6$   .�66   Serving files from:6K    �� 5&      -   5         ��Name$ RootDirectoryTextRun��5&      �� 5'      ��Edit FileRootDirectory property in configuration file (.exe.config) and restart server application to serve files from a different directory.5)      �� 5*      -   5        $	 NotServingFilesText��5*      $	��0 5q�65   $��14(�6B   $
��Bold"�6P   $y�	Collapsed=�6b   Not Serving Files5,      5-      9� 5.   
   $��1��6   $��0��6    $��3��60   $��Stretch=�6D   $��Stretch=�6b   $��Stretch=�6~   $��5 0 5 5q�6�    9� ExpandDirection$
 Down=�5/       9� 
IsExpanded . $��Errors:��50      $��Black"�6$   .��51      ��  �� 
IsReadOnly . " �� VerticalScrollBarVisibility$ Visible=�6(   $ �� HorizontalScrollBarVisibility$
 Auto=�6N   $	��200��6s   $��10(�52      ��6$   �� $ 	ErrorText�53      54      55      