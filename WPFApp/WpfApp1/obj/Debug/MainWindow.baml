   M S B A M L   `   `   `  ����B  >WpfApp1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null!clr-namespace:WpfApp1WpfApp1  5   	       WpfApp1.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   'localclr-namespace:WpfApp1     5   	   $	��600��5   	   $	��920��5	   	     � Title$  Real Time Chat��5
   	   .��5      � ���#323233.+�5   
   ��  �� CornerRadius$ 10��6   ���#1e1e1f
�������$��0q�5   	   $��35��5   	   $��230,0,25,10q�5   	   $��Bottom=�5   	   $��Stretch=�5   	   ._�5      � .+�5      �� -   5         ��Name$ PlaceholderText��5      $��Scrivi un messaggio ...��5      
�������$��Center=�5       �� Padding$ 	10,0,10,0q�5      $��14(�5       J� IsHitTestVisible .  $y�Visible=�5      $��5,0,0,0q�5      �� 5!      -   5         ��Name$ ChatTextBox��5*      
������ 
�������$��0q�5#      $��Center=�5$      $��	10,0,10,0q�5%      $��14(�5&      $��Stretch=�5'      5*      5+      5,      �� 5-   
   ���#1e1e1f$��1q�5.   	   $��
10,10,0,10q�5/   	   $	��200��50   	   $
��Left=�51   	   $��Stretch=�52   	   $ 10��53   	   ._�54      �� $��
Lista Chat��6   
�������$��Center=�56      $	��Top=�57       J� RenderTransformOrigin$ 1.688,0.735&�58      $��0,9,0,0q�59      5:      �� 5<   
   -   5         ��Name$ MessagesScrollViewer��5<      $
��Auto=�65   $��230,10,70,80q�6X   .��5=      �� -   5         ��Name$ MessagesContainer��5=      ��5>      �� 5?      .R�5@      �� ���#1e1e1f$ 5��5A      $��5q�5B      	 �� Padding$	 10q�5C      ._�5D      �� .+�5E   "   �� 
�������$
��Bold"�5G   $   $��0,0,0,5q�5H   $   ��5E   ,   �� *Author+5H   $   �� 5I   "   
���������6,   �� *Body+5J   $   5K      5L      5M      5O      5P      �� 5R   
   -   5        $ ChatListScrollViewer��5R      $
��Auto=�65   $��20,60,720,20q�6X   .��5S      �� -   5        $ ChatContainer��5S      ��5T      �� 5U      .R�5V      �� ���#1e1e1f$ 5��5W      $��5q�5X      $	 10q�5Y      ._�5Z      �� .+�5[   "   �� 
�������$
��Bold"�5]      $��0,0,0,5q�5^      ��5[   ,   �� *Author+5^      �� 5_   "   
���������6,   �� *Body+5`      5a      5b      5c      5e      5f      5j      5k      