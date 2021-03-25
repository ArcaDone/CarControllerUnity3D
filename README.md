# CarControllerUnity3D
 Unity3D - The easiest way to drive your car upside down. @CarController @Spline @InversePendulum @Stabilizer @Post-processing @led light

![CarController](https://user-images.githubusercontent.com/38981338/112488999-55371100-8d7e-11eb-9d02-227aaaf62130.png)

 
 ## Zero Gravity (Unity 3D)

Within this project you will find all the elements to achieve local gravity while you are playing with your car.
Everything you find inside can be copied and reused for free within your project. You will also find the car controller script (CarController.cs), and a script to be able to save as prefab the Meshes built using Spline techniques. The latter allows you to create any type of prefab based on the mesh you created using Spline.

## Hot it Work
The operation of Zero Gravity is contained in this function:

```bash
    void AttractorCheck()
    {
        int layerMask = 1 << 8;

        layerMask = ~layerMask;
        RaycastHit hit;
        if (Physics.Raycast(RayCastTransf.position, transform.TransformDirection(Vector3.down), out hit, 5, layerMask) && hit.transform.CompareTag("AttractionLayer"))
        {
            Debug.DrawRay(RayCastTransf.position, transform.TransformDirection(Vector3.down) * hit.distance, Color.yellow);
            _rb.useGravity = false;
            ApplyStabilizer(hit.normal);
            _rb.AddRelativeForce(Vector3.down * 10000 * attractionSpeed * Time.fixedDeltaTime);
        }
        else
        {
            Debug.DrawRay(RayCastTransf.position, transform.TransformDirection(Vector3.down) * 5, Color.white);
            _rb.useGravity = true;
            ApplyStabilizer(Vector3.up);
        }
    }
```

Through Raycast a check is carried out under the car to see if we are close to a layer of zero gravity. 
The check is successful if the GameObject in question has the "AttractionLayer" tag. 
In this case all we have to do is remove the gravity, apply a normal force to the surface directed downwards in the car and that's it. 
Now we can go our own way without worrying about "standard" gravity.

 
 ![Schermata 2021-03-25 alle 14 48 27](https://user-images.githubusercontent.com/38981338/112486845-8c0c2780-8d7c-11eb-93f6-63805b1247c6.png)
 
 ![Schermata 2021-03-25 alle 14 50 02](https://user-images.githubusercontent.com/38981338/112487053-b958d580-8d7c-11eb-9eb9-23815f5c537c.png)



 ##In addition to this

The reverse pendulum is also applied to keep the car always straight with respect to the surface and thus prevent it from slipping. 
The function is as follows:

```bash
    void Stabilizer(Vector3 surface)
    {
        Rigidbody ms_Rigidbody = gameObject.GetComponent<Rigidbody>();
        Vector3 axisFromRotate = Vector3.Cross(transform.up, surface);
        Vector3 torqueForce = axisFromRotate.normalized * axisFromRotate.magnitude * _stabilizerMotorTorque;
        torqueForce.x = torqueForce.x * 0.4f;
        torqueForce -= ms_Rigidbody.angularVelocity;

        ms_Rigidbody.AddTorque((torqueForce * 2 / 3 ) * ms_Rigidbody.mass * 0.02f, ForceMode.Impulse);
    }
```


#### ========================================================================================================================
