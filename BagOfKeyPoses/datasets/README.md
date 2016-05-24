Datasets
=============

Here are the two datasets used for our examples.
 
MSR Action Recognition - MSR
----------------------
http://research.microsoft.com/en-us/um/people/zliu/actionrecorsrc/

We used the Skeleton Data in real world coordinates from "MSR Action3D Dataset" with 20 action types and 10 subjects.
Direct download : http://research.microsoft.com/en-us/um/people/zliu/actionrecorsrc/MSRAction3DSkeletonReal3D.rar

Only the a13_s09_e02_skeleton3D.txt file is removed during the loading because it contains only zeros.

Actions as Space-Time Shapes - Weizmann
----------------------------
http://www.wisdom.weizmann.ac.il/~vision/SpaceTimeActions.html

We used the matlab file that contains the extracted masks obtained by background subtraction, we specifically used the aligned masks.
Direct download : http://www.wisdom.weizmann.ac.il/~vision/VideoAnalysis/Demos/SpaceTimeActions/DB/classification_masks.mat

We used the imcontour function to create a contour plot of the image data (http://uk.mathworks.com/help/images/ref/imcontour.html?requestedDomain=www.mathworks.com)
Then the contours are saved into text files and we used them as inputs for our algorithm.

License
-------

**Weizmann** 
_Gorelick, L., Blank, M., Shechtman, E., Irani, M., & Basri, R. (2007). Actions as space-time shapes. Pattern Analysis and Machine Intelligence, IEEE Transactions on, 29(12), 2247-2253._

**MSR**
_W. Li, Z. Zhang, and Z. Liu. (2010). Action recognition based on a bag of 3d points. In Human Communicative Behavior Analysis Workshop (in conjunction with CVPR),  2, 5, 6_