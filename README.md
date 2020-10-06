# Bonsai.Miniscope
[Bonsai](http://bonsai-rx.org/) package for controlling and acquiring data from head-borne miniscopes for calcium imaging. 

It contains the following nodes: 

- `UCLAMiniscope`: UCLA Miniscope V3 using legacy DAQ firmware. Use this if you have an old DAQ box and have not updated firmware recently.
- `UCLAMiniscopeV3`: UCLA Miniscope V3 using updated DAQ box and firmware. Use this if you are also using the V4 scope.
- `UCLAMiniscopeV4`: UCLA Miniscope V4 with integrated IMU.
    - **NOTE** The jumpy, corrupt IMU data is a known issue and is also present in the official Miniscope software, it is just filtered and dumped before it is shown on the screen. 
      Doing this is against the philosophy of Bonsai (source nodes should be as pure a representation of hardware as possible), so we have chosen to expose the whole data stream. 
      I've submitted an issue requesting a firmware fix for this issue. In the meantime, filtering (e.g. by making sure the norm of the quaternion is always the same value) can be done 
      downstream.

Feel free to submit an issue or PR if you want to add your Miniscope to this package.

This project was created and is maintained by Jonathan P. Newman. If you use it in your work, please reference this repository in any papers or presentations.

## Upstream
This project is a fork of [Bonsai.Vision.CameraCapture](https://bitbucket.org/horizongir/bonsai/src/43c4072273efcaff77e429296c2d6d8756ec07c7/Bonsai.Vision/CameraCapture.cs?at=default&fileviewer=file-view-default)

## License
[MIT](https://opensource.org/licenses/MIT)