# Bonsai.Miniscope
[Bonsai](http://bonsai-rx.org/) package for controlling and acquiring data from head-borne miniscopes for calcium imaging. 

It contains the following nodes: 

- `UCLAMiniscope`: UCLA Miniscope V3 using legacy DAQ firmware. Use this if you have an old DAQ box and have not updated firmware recently.
- `UCLAMiniscopeV3`: UCLA Miniscope V3 using updated DAQ box and firmware. Use this if you are also using the V4 scope.
- `UCLAMiniscopeV4`: UCLA Miniscope V4 with integrated IMU.
    - **NOTE** On some V4 miniscopes, the IMU data will appear jumpy with some or all of the components taking the same value. This is a known issue, and has been addressed in later versions 
      of the Miniscope hardware. See [this issue](https://github.com/Aharoni-Lab/Miniscope-DAQ-QT-Software/issues/23) for more information.

Feel free to submit an issue or PR if you want to add your Miniscope to this package.

This project was created and is maintained by Jonathan P. Newman. If you use it in your work, please reference this repository in any papers or presentations.

## Upstream
This project is a fork of [Bonsai.Vision.CameraCapture](https://bitbucket.org/horizongir/bonsai/src/43c4072273efcaff77e429296c2d6d8756ec07c7/Bonsai.Vision/CameraCapture.cs?at=default&fileviewer=file-view-default)

## License
[MIT](https://opensource.org/licenses/MIT)