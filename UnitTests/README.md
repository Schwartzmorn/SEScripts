**To have the unit test working, the following lines should be added at the end of the whitelist.cache:**
```
# Added for Unit Tests
System.Diagnostics.Debug+*, System
Microsoft.VisualStudio.TestTools.UnitTesting.*, Microsoft.VisualStudio.QualityTools.UnitTestFramework
System.Type.GetMethod(string, System.Reflection.BindingFlags), mscorlib
System.Type.GetMethods(System.Reflection.BindingFlags), mscorlib
System.Reflection.BindingFlags+*, mscorlib
System.Reflection.MethodInfo, mscorlib
System.Reflection.MethodBase.Invoke(object, object[]), mscorlib
System.Reflection.TargetInvocationException, mscorlib
```
