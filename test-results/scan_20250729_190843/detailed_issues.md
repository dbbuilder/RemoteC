# Detailed Test Issues Report

## Failed Tests Analysis

### Unit Test Failures
[xUnit.net 00:00:00.83]   Finished:    RemoteC.Tests.Unit
  Passed RemoteC.Tests.Unit.Host.Services.SessionManagerTests.ValidateSessionAsync_WithoutPermission_ShouldReturnFalse [5 ms]
  Failed RemoteC.Tests.Unit.Host.Services.ScreenCaptureServiceTests.CaptureScreenAsync_WithScaling_ShouldApplyScale [180 ms]
  Error Message:
   Expected result not to be <null>.
  Stack Trace:
     at FluentAssertions.Execution.XUnit2TestFramework.Throw(String message)
   at FluentAssertions.Execution.TestFrameworkProvider.Throw(String message)
   at FluentAssertions.Execution.DefaultAssertionStrategy.HandleFailure(String message)
   at FluentAssertions.Execution.AssertionScope.FailWith(Func`1 failReasonFunc)
   at FluentAssertions.Execution.AssertionScope.FailWith(Func`1 failReasonFunc)
   at FluentAssertions.Execution.AssertionScope.FailWith(String message)
   at FluentAssertions.Primitives.ReferenceTypeAssertions`2.NotBeNull(String because, Object[] becauseArgs)
--
  Passed RemoteC.Tests.Unit.Host.Services.ScreenCaptureServiceTests.InitializeAsync_WhenProviderFailsToInitialize_ShouldThrowException [1 ms]
  Passed RemoteC.Tests.Unit.Host.Services.ScreenCaptureServiceTests.CaptureScreenAsync_WhenProviderReturnsNull_ShouldReturnNull [2 ms]
  Failed RemoteC.Tests.Unit.Host.Services.ScreenCaptureServiceTests.CaptureScreenAsync_WhenInitialized_ShouldReturnScreenData [9 ms]
  Error Message:
   Expected result not to be <null>.
  Stack Trace:
     at FluentAssertions.Execution.XUnit2TestFramework.Throw(String message)
   at FluentAssertions.Execution.TestFrameworkProvider.Throw(String message)
   at FluentAssertions.Execution.DefaultAssertionStrategy.HandleFailure(String message)
   at FluentAssertions.Execution.AssertionScope.FailWith(Func`1 failReasonFunc)
   at FluentAssertions.Execution.AssertionScope.FailWith(Func`1 failReasonFunc)
   at FluentAssertions.Execution.AssertionScope.FailWith(String message)
   at FluentAssertions.Primitives.ReferenceTypeAssertions`2.NotBeNull(String because, Object[] becauseArgs)

### API Test Failures
[xUnit.net 00:00:00.51]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PinServiceTests.cs(28,0): at RemoteC.Api.Tests.Services.PinServiceTests..ctor()
[xUnit.net 00:00:00.51]            at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.PinServiceTests.ValidatePinAsync_WithCorrectPin_ReturnsTrue [1 ms]
  Error Message:
   System.NotSupportedException : Unsupported expression: c => c.GetValue<int>("Security:PinLength", 6)
Extension methods (here: ConfigurationBinder.GetValue) may not be used in setup / verification expressions.
  Stack Trace:
     at Moq.Guard.IsOverridable(MethodInfo method, Expression expression) in /_/src/Moq/Guard.cs:line 87
   at Moq.MethodExpectation..ctor(LambdaExpression expression, MethodInfo method, IReadOnlyList`1 arguments, Boolean exactGenericTypeArguments, Boolean skipMatcherInitialization, Boolean allowNonOverridable) in /_/src/Moq/MethodExpectation.cs:line 236
   at Moq.ExpressionExtensions.<Split>g__Split|5_0(Expression e, Expression& r, MethodExpectation& p, Boolean assignment, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 256
   at Moq.ExpressionExtensions.Split(LambdaExpression expression, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 170
   at Moq.Mock.SetupRecursive[TSetup](Mock mock, LambdaExpression expression, Func`4 setupLast, Boolean allowNonOverridableLastProperty) in /_/src/Moq/Mock.cs:line 728
   at Moq.Mock.Setup(Mock mock, LambdaExpression expression, Condition condition) in /_/src/Moq/Mock.cs:line 562
--
   at RemoteC.Api.Tests.Services.PinServiceTests..ctor() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PinServiceTests.cs:line 28
   at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.PinServiceTests.IsPinValidAsync_WithValidUnusedPin_ReturnsTrue [1 ms]
  Error Message:
   System.NotSupportedException : Unsupported expression: c => c.GetValue<int>("Security:PinLength", 6)
Extension methods (here: ConfigurationBinder.GetValue) may not be used in setup / verification expressions.
  Stack Trace:
     at Moq.Guard.IsOverridable(MethodInfo method, Expression expression) in /_/src/Moq/Guard.cs:line 87
   at Moq.MethodExpectation..ctor(LambdaExpression expression, MethodInfo method, IReadOnlyList`1 arguments, Boolean exactGenericTypeArguments, Boolean skipMatcherInitialization, Boolean allowNonOverridable) in /_/src/Moq/MethodExpectation.cs:line 236
   at Moq.ExpressionExtensions.<Split>g__Split|5_0(Expression e, Expression& r, MethodExpectation& p, Boolean assignment, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 256
   at Moq.ExpressionExtensions.Split(LambdaExpression expression, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 170
   at Moq.Mock.SetupRecursive[TSetup](Mock mock, LambdaExpression expression, Func`4 setupLast, Boolean allowNonOverridableLastProperty) in /_/src/Moq/Mock.cs:line 728
   at Moq.Mock.Setup(Mock mock, LambdaExpression expression, Condition condition) in /_/src/Moq/Mock.cs:line 562
--
   at RemoteC.Api.Tests.Services.PinServiceTests..ctor() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PinServiceTests.cs:line 28
   at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.PinServiceTests.ValidatePinAsync_WithUsedPin_ReturnsFalse [1 ms]
  Error Message:
   System.NotSupportedException : Unsupported expression: c => c.GetValue<int>("Security:PinLength", 6)
Extension methods (here: ConfigurationBinder.GetValue) may not be used in setup / verification expressions.
  Stack Trace:
     at Moq.Guard.IsOverridable(MethodInfo method, Expression expression) in /_/src/Moq/Guard.cs:line 87
   at Moq.MethodExpectation..ctor(LambdaExpression expression, MethodInfo method, IReadOnlyList`1 arguments, Boolean exactGenericTypeArguments, Boolean skipMatcherInitialization, Boolean allowNonOverridable) in /_/src/Moq/MethodExpectation.cs:line 236
   at Moq.ExpressionExtensions.<Split>g__Split|5_0(Expression e, Expression& r, MethodExpectation& p, Boolean assignment, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 256
   at Moq.ExpressionExtensions.Split(LambdaExpression expression, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 170
   at Moq.Mock.SetupRecursive[TSetup](Mock mock, LambdaExpression expression, Func`4 setupLast, Boolean allowNonOverridableLastProperty) in /_/src/Moq/Mock.cs:line 728
   at Moq.Mock.Setup(Mock mock, LambdaExpression expression, Condition condition) in /_/src/Moq/Mock.cs:line 562
--
   at RemoteC.Api.Tests.Services.PinServiceTests..ctor() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PinServiceTests.cs:line 28
   at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.PinServiceTests.GeneratePinAsync_GeneratesCorrectLengthPin [1 ms]
  Error Message:
   System.NotSupportedException : Unsupported expression: c => c.GetValue<int>("Security:PinLength", 6)
Extension methods (here: ConfigurationBinder.GetValue) may not be used in setup / verification expressions.
  Stack Trace:
     at Moq.Guard.IsOverridable(MethodInfo method, Expression expression) in /_/src/Moq/Guard.cs:line 87
   at Moq.MethodExpectation..ctor(LambdaExpression expression, MethodInfo method, IReadOnlyList`1 arguments, Boolean exactGenericTypeArguments, Boolean skipMatcherInitialization, Boolean allowNonOverridable) in /_/src/Moq/MethodExpectation.cs:line 236
   at Moq.ExpressionExtensions.<Split>g__Split|5_0(Expression e, Expression& r, MethodExpectation& p, Boolean assignment, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 256
   at Moq.ExpressionExtensions.Split(LambdaExpression expression, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 170
   at Moq.Mock.SetupRecursive[TSetup](Mock mock, LambdaExpression expression, Func`4 setupLast, Boolean allowNonOverridableLastProperty) in /_/src/Moq/Mock.cs:line 728
   at Moq.Mock.Setup(Mock mock, LambdaExpression expression, Condition condition) in /_/src/Moq/Mock.cs:line 562
--
   at RemoteC.Api.Tests.Services.PinServiceTests..ctor() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PinServiceTests.cs:line 28
   at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.PinServiceTests.ValidatePinAsync_WithIncorrectPin_ReturnsFalse [1 ms]
  Error Message:
   System.NotSupportedException : Unsupported expression: c => c.GetValue<int>("Security:PinLength", 6)
Extension methods (here: ConfigurationBinder.GetValue) may not be used in setup / verification expressions.
  Stack Trace:
     at Moq.Guard.IsOverridable(MethodInfo method, Expression expression) in /_/src/Moq/Guard.cs:line 87
   at Moq.MethodExpectation..ctor(LambdaExpression expression, MethodInfo method, IReadOnlyList`1 arguments, Boolean exactGenericTypeArguments, Boolean skipMatcherInitialization, Boolean allowNonOverridable) in /_/src/Moq/MethodExpectation.cs:line 236
   at Moq.ExpressionExtensions.<Split>g__Split|5_0(Expression e, Expression& r, MethodExpectation& p, Boolean assignment, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 256
   at Moq.ExpressionExtensions.Split(LambdaExpression expression, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 170
   at Moq.Mock.SetupRecursive[TSetup](Mock mock, LambdaExpression expression, Func`4 setupLast, Boolean allowNonOverridableLastProperty) in /_/src/Moq/Mock.cs:line 728
   at Moq.Mock.Setup(Mock mock, LambdaExpression expression, Condition condition) in /_/src/Moq/Mock.cs:line 562
--
   at RemoteC.Api.Tests.Services.PinServiceTests..ctor() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PinServiceTests.cs:line 28
   at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.PinServiceTests.ValidatePinAsync_WithNonExistentPin_ReturnsFalse [1 ms]
  Error Message:
   System.NotSupportedException : Unsupported expression: c => c.GetValue<int>("Security:PinLength", 6)
Extension methods (here: ConfigurationBinder.GetValue) may not be used in setup / verification expressions.
  Stack Trace:
     at Moq.Guard.IsOverridable(MethodInfo method, Expression expression) in /_/src/Moq/Guard.cs:line 87
   at Moq.MethodExpectation..ctor(LambdaExpression expression, MethodInfo method, IReadOnlyList`1 arguments, Boolean exactGenericTypeArguments, Boolean skipMatcherInitialization, Boolean allowNonOverridable) in /_/src/Moq/MethodExpectation.cs:line 236
   at Moq.ExpressionExtensions.<Split>g__Split|5_0(Expression e, Expression& r, MethodExpectation& p, Boolean assignment, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 256
   at Moq.ExpressionExtensions.Split(LambdaExpression expression, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 170
   at Moq.Mock.SetupRecursive[TSetup](Mock mock, LambdaExpression expression, Func`4 setupLast, Boolean allowNonOverridableLastProperty) in /_/src/Moq/Mock.cs:line 728
   at Moq.Mock.Setup(Mock mock, LambdaExpression expression, Condition condition) in /_/src/Moq/Mock.cs:line 562
--
   at RemoteC.Api.Tests.Services.PinServiceTests..ctor() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PinServiceTests.cs:line 28
   at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.PinServiceTests.GeneratePinAsync_StoresHashedPinInCache [1 ms]
  Error Message:
   System.NotSupportedException : Unsupported expression: c => c.GetValue<int>("Security:PinLength", 6)
Extension methods (here: ConfigurationBinder.GetValue) may not be used in setup / verification expressions.
  Stack Trace:
     at Moq.Guard.IsOverridable(MethodInfo method, Expression expression) in /_/src/Moq/Guard.cs:line 87
   at Moq.MethodExpectation..ctor(LambdaExpression expression, MethodInfo method, IReadOnlyList`1 arguments, Boolean exactGenericTypeArguments, Boolean skipMatcherInitialization, Boolean allowNonOverridable) in /_/src/Moq/MethodExpectation.cs:line 236
   at Moq.ExpressionExtensions.<Split>g__Split|5_0(Expression e, Expression& r, MethodExpectation& p, Boolean assignment, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 256
   at Moq.ExpressionExtensions.Split(LambdaExpression expression, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 170
   at Moq.Mock.SetupRecursive[TSetup](Mock mock, LambdaExpression expression, Func`4 setupLast, Boolean allowNonOverridableLastProperty) in /_/src/Moq/Mock.cs:line 728
   at Moq.Mock.Setup(Mock mock, LambdaExpression expression, Condition condition) in /_/src/Moq/Mock.cs:line 562
--
[xUnit.net 00:00:00.53]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PinServiceTests.cs(28,0): at RemoteC.Api.Tests.Services.PinServiceTests..ctor()
[xUnit.net 00:00:00.53]            at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.PinServiceTests.InvalidatePinAsync_RemovesPinFromCache [1 ms]
  Error Message:
   System.NotSupportedException : Unsupported expression: c => c.GetValue<int>("Security:PinLength", 6)
Extension methods (here: ConfigurationBinder.GetValue) may not be used in setup / verification expressions.
  Stack Trace:
     at Moq.Guard.IsOverridable(MethodInfo method, Expression expression) in /_/src/Moq/Guard.cs:line 87
   at Moq.MethodExpectation..ctor(LambdaExpression expression, MethodInfo method, IReadOnlyList`1 arguments, Boolean exactGenericTypeArguments, Boolean skipMatcherInitialization, Boolean allowNonOverridable) in /_/src/Moq/MethodExpectation.cs:line 236
   at Moq.ExpressionExtensions.<Split>g__Split|5_0(Expression e, Expression& r, MethodExpectation& p, Boolean assignment, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 256
   at Moq.ExpressionExtensions.Split(LambdaExpression expression, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 170
   at Moq.Mock.SetupRecursive[TSetup](Mock mock, LambdaExpression expression, Func`4 setupLast, Boolean allowNonOverridableLastProperty) in /_/src/Moq/Mock.cs:line 728
   at Moq.Mock.Setup(Mock mock, LambdaExpression expression, Condition condition) in /_/src/Moq/Mock.cs:line 562
--
   at RemoteC.Api.Tests.Services.PinServiceTests..ctor() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PinServiceTests.cs:line 28
   at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.PinServiceTests.GeneratePinAsync_UsesConfiguredPinLength [1 ms]
  Error Message:
   System.NotSupportedException : Unsupported expression: c => c.GetValue<int>("Security:PinLength", 6)
Extension methods (here: ConfigurationBinder.GetValue) may not be used in setup / verification expressions.
  Stack Trace:
     at Moq.Guard.IsOverridable(MethodInfo method, Expression expression) in /_/src/Moq/Guard.cs:line 87
   at Moq.MethodExpectation..ctor(LambdaExpression expression, MethodInfo method, IReadOnlyList`1 arguments, Boolean exactGenericTypeArguments, Boolean skipMatcherInitialization, Boolean allowNonOverridable) in /_/src/Moq/MethodExpectation.cs:line 236
   at Moq.ExpressionExtensions.<Split>g__Split|5_0(Expression e, Expression& r, MethodExpectation& p, Boolean assignment, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 256
   at Moq.ExpressionExtensions.Split(LambdaExpression expression, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 170
   at Moq.Mock.SetupRecursive[TSetup](Mock mock, LambdaExpression expression, Func`4 setupLast, Boolean allowNonOverridableLastProperty) in /_/src/Moq/Mock.cs:line 728
   at Moq.Mock.Setup(Mock mock, LambdaExpression expression, Condition condition) in /_/src/Moq/Mock.cs:line 562
--
  Passed RemoteC.Api.Tests.Services.EncryptionServiceTests.EncryptDecrypt_RoundTrip_WorksCorrectly(testText: "") [1 ms]
  Passed RemoteC.Api.Tests.Services.EncryptionServiceTests.EncryptDecrypt_RoundTrip_WorksCorrectly(testText: "Short text") [< 1 ms]
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.VerifySignatureAsync_ValidatesCorrectSignature [206 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateSigningKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 276
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.VerifySignatureAsync_ValidatesCorrectSignature() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 311
--- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.EncryptionServiceTests.GenerateKeyAsync_GeneratesUniqueKeys [7 ms]
  Passed RemoteC.Api.Tests.Services.EncryptionServiceTests.EncryptAsync_WithValidData_ReturnsEncryptedData [2 ms]
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.RotateSessionKeysAsync_GeneratesNewKeys [6 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.CreateTestSessionKeys(Nullable`1 sessionId) in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 476
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.RotateSessionKeysAsync_GeneratesNewKeys() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 374
--- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.EncryptionServiceTests.RotateKeysAsync_OldKeysStillWorkForDecryption [4 ms]
[xUnit.net 00:00:00.62]     RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.DecryptAsync_FailsWithWrongKey [FAIL]
--
[xUnit.net 00:00:00.64]         --- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.EncryptionServiceTests.GenerateKeyAsync_GeneratedKeysAreUsable [1 ms]
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.DecryptAsync_FailsWithWrongKey [2 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.CreateTestSessionKeys(Nullable`1 sessionId) in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 476
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.DecryptAsync_FailsWithWrongKey() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 151
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.GenerateCertificateAsync_CreatesValidCertificate [2 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateSigningKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 276
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.GenerateCertificateAsync_CreatesValidCertificate() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 413
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.VerifySignatureAsync_RejectsTamperedData [2 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateSigningKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 276
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.VerifySignatureAsync_RejectsTamperedData() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 329
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.PerformKeyExchangeAsync_GeneratesSharedSecret [3 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.PerformKeyExchangeAsync_GeneratesSharedSecret() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 63
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.ValidateCertificateAsync_AcceptsValidCertificate [2 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateSigningKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 276
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.ValidateCertificateAsync_AcceptsValidCertificate() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 437
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.DecryptAsync_FailsWithTamperedCiphertext [2 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.CreateTestSessionKeys(Nullable`1 sessionId) in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 476
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.DecryptAsync_FailsWithTamperedCiphertext() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 165
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EncryptionServiceTests.EncryptAsync_WithEmptyData_ReturnsEmptyArray [15 ms]
  Error Message:
   Assert.Empty() Failure: Collection was not empty
Collection: [91, 213, 180, 70, 2, ···]
  Stack Trace:
     at RemoteC.Api.Tests.Services.EncryptionServiceTests.EncryptAsync_WithEmptyData_ReturnsEmptyArray() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EncryptionServiceTests.cs:line 61
--- End of stack trace from previous location ---
[xUnit.net 00:00:00.65]     RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.EncryptAsync_ProducesValidCiphertext [FAIL]
[xUnit.net 00:00:00.65]       System.InvalidOperationException : The key cannot be exported.
[xUnit.net 00:00:00.65]       Stack Trace:
[xUnit.net 00:00:00.65]            at NSec.Cryptography.Key.Export(KeyBlobFormat format)
--
  Passed RemoteC.Api.Tests.Services.EncryptionServiceTests.DecryptAsync_WithValidEncryptedData_ReturnsOriginalData [1 ms]
  Passed RemoteC.Api.Tests.Services.EncryptionServiceTests.RevokeKeyAsync_WithNonExistentKey_DoesNotThrow [2 ms]
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.EncryptAsync_ProducesValidCiphertext [5 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.CreateTestSessionKeys(Nullable`1 sessionId) in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 476
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.EncryptAsync_ProducesValidCiphertext() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 114
--- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.ConfigureFederationAsync_AddsIdentityProvider [257 ms]
  Passed RemoteC.Api.Tests.Services.EncryptionServiceTests.EncryptAsync_WithInvalidKeyId_ThrowsException [2 ms]
--
[xUnit.net 00:00:00.67]         --- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.DockerServiceTests.ValidateNodeAsync_WithAnyAddress_ReturnsTrue [7 ms]
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.EstablishSessionKeysAsync_CreatesValidSessionKeys [2 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.EstablishSessionKeysAsync_CreatesValidSessionKeys() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 87
--- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.DockerServiceTests.UpdateContainerAsync_ReturnsTrue [10 ms]
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.DecryptStreamAsync_RecoverLargeData [2 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.CreateTestSessionKeys(Nullable`1 sessionId) in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 476
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.DecryptStreamAsync_RecoverLargeData() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 247
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.IsKeyRotationRequiredAsync_DetectsExpiredKeys [7 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.CreateTestSessionKeys(Nullable`1 sessionId) in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 476
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.IsKeyRotationRequiredAsync_DetectsExpiredKeys() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 391
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.VerifySignatureAsync_RejectsWrongPublicKey [2 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateSigningKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 276
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.VerifySignatureAsync_RejectsWrongPublicKey() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 350
--- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.EncryptionServiceTests.EncryptDecrypt_LargeData_WorksCorrectly [27 ms]
[xUnit.net 00:00:00.68]     RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.DecryptAsync_FailsWithTamperedTag [FAIL]
[xUnit.net 00:00:00.68]       System.InvalidOperationException : The key cannot be exported.
--
[xUnit.net 00:00:00.69]         --- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.DockerServiceTests.MigrateContainerAsync_LogsCorrectInformation [15 ms]
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.DecryptAsync_FailsWithTamperedTag [3 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.CreateTestSessionKeys(Nullable`1 sessionId) in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 476
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.DecryptAsync_FailsWithTamperedTag() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 181
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.EncryptAsync_UniqueNoncePerMessage [2 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.CreateTestSessionKeys(Nullable`1 sessionId) in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 476
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.EncryptAsync_UniqueNoncePerMessage() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 197
--- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.EncryptionServiceTests.RevokeKeyAsync_AfterRevocation_EncryptionFails [6 ms]
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.DecryptAsync_RecoverOriginalPlaintext [2 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.CreateTestSessionKeys(Nullable`1 sessionId) in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 476
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.DecryptAsync_RecoverOriginalPlaintext() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 135
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.CacheServiceTests.GetOrCreateAsync_WithCachedValue_ReturnsCachedValue [294 ms]
  Error Message:
   Assert.Equal() Failure: Values differ
Expected: 1
Actual:   2
  Stack Trace:
     at RemoteC.Api.Tests.Services.CacheServiceTests.GetOrCreateAsync_WithCachedValue_ReturnsCachedValue() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/CacheServiceTests.cs:line 245
--- End of stack trace from previous location ---
[xUnit.net 00:00:00.69]     RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.GenerateSigningKeyPairAsync_CreatesValidEd25519KeyPair [FAIL]
[xUnit.net 00:00:00.69]       System.InvalidOperationException : The key cannot be exported.
[xUnit.net 00:00:00.69]       Stack Trace:
--
[xUnit.net 00:00:00.71]         --- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.EncryptionServiceTests.DecryptAsync_WithWrongKey_ThrowsException [8 ms]
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.GenerateSigningKeyPairAsync_CreatesValidEd25519KeyPair [5 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateSigningKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 276
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.GenerateSigningKeyPairAsync_CreatesValidEd25519KeyPair() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 282
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.SignDataAsync_ProducesValidSignature [3 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateSigningKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 276
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.SignDataAsync_ProducesValidSignature() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 296
--- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.DockerServiceTests.GetLoadBalancerInfoAsync_ReturnsLoadBalancerInfo [23 ms]
  Failed RemoteC.Api.Tests.Services.CacheServiceTests.SetAsync_WithValidValue_StoresInCache [14 ms]
  Error Message:
   Assert.NotNull() Failure: Value is null
  Stack Trace:
     at RemoteC.Api.Tests.Services.CacheServiceTests.SetAsync_WithValidValue_StoresInCache() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/CacheServiceTests.cs:line 118
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.GenerateKeyPairAsync_CreatesValidX25519KeyPair [9 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.GenerateKeyPairAsync_CreatesValidX25519KeyPair() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 47
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.CacheServiceTests.GetStringAsync_WithExistingKey_ReturnsString [5 ms]
  Error Message:
   System.NotSupportedException : Unsupported expression: c => c.GetStringAsync(key, It.IsAny<CancellationToken>())
Extension methods (here: DistributedCacheExtensions.GetStringAsync) may not be used in setup / verification expressions.
  Stack Trace:
     at Moq.Guard.IsOverridable(MethodInfo method, Expression expression) in /_/src/Moq/Guard.cs:line 87
   at Moq.MethodExpectation..ctor(LambdaExpression expression, MethodInfo method, IReadOnlyList`1 arguments, Boolean exactGenericTypeArguments, Boolean skipMatcherInitialization, Boolean allowNonOverridable) in /_/src/Moq/MethodExpectation.cs:line 236
   at Moq.ExpressionExtensions.<Split>g__Split|5_0(Expression e, Expression& r, MethodExpectation& p, Boolean assignment, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 256
   at Moq.ExpressionExtensions.Split(LambdaExpression expression, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 170
   at Moq.Mock.SetupRecursive[TSetup](Mock mock, LambdaExpression expression, Func`4 setupLast, Boolean allowNonOverridableLastProperty) in /_/src/Moq/Mock.cs:line 728
   at Moq.Mock.Setup(Mock mock, LambdaExpression expression, Condition condition) in /_/src/Moq/Mock.cs:line 562
--
[xUnit.net 00:00:00.74]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/CacheServiceTests.cs(304,0): at RemoteC.Api.Tests.Services.CacheServiceTests.InvalidatePatternAsync_LogsWarning()
[xUnit.net 00:00:00.74]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.EncryptStreamAsync_HandlesLargeData [3 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 42
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.CreateTestSessionKeys(Nullable`1 sessionId) in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 476
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.EncryptStreamAsync_HandlesLargeData() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 222
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.ValidateCertificateAsync_RejectsExpiredCertificate [4 ms]
  Error Message:
   System.InvalidOperationException : The key cannot be exported.
  Stack Trace:
     at NSec.Cryptography.Key.Export(KeyBlobFormat format)
   at RemoteC.Api.Services.E2EEncryptionService.GenerateSigningKeyPairAsync() in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/E2EEncryptionService.cs:line 276
   at RemoteC.Api.Tests.Services.E2EEncryptionServiceTests.ValidateCertificateAsync_RejectsExpiredCertificate() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/E2EEncryptionServiceTests.cs:line 454
--- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.CacheServiceTests.GetAsync_WithNonExistingKey_ReturnsNull [9 ms]
  Passed RemoteC.Api.Tests.Services.DockerServiceTests.GetLoadBalancerStatsAsync_ReturnsStats [26 ms]
  Passed RemoteC.Api.Tests.Services.DockerServiceTests.DeployContainerAsync_WithValidSpec_ReturnsContainerInfo [3 ms]
  Passed RemoteC.Api.Tests.Services.DockerServiceTests.ConfigureLoadBalancerAsync_ReturnsTrue [2 ms]
  Passed RemoteC.Api.Tests.Services.DockerServiceTests.DrainNodeAsync_ReturnsTrue [2 ms]
  Failed RemoteC.Api.Tests.Services.CacheServiceTests.InvalidatePatternAsync_LogsWarning [24 ms]
  Error Message:
   Moq.MockException : 
Expected invocation on the mock once, but was 0 times: x => x.Log<It.IsAnyType>(LogLevel.Warning, It.IsAny<EventId>(), It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Pattern-based cache invalidation not implemented")), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>())

Performed invocations:

   Mock<ILogger<CacheService>:5> (x):

      ILogger.Log<FormattedLogValues>(LogLevel.Warning, 0, Pattern-based cache invalidation requested for pattern: test:* - not supported with IMemoryCache, null, Func<FormattedLogValues, Exception, string>)

--
[xUnit.net 00:00:00.96]         --- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.DockerServiceTests.GetContainerHealthAsync_ReturnsHealthyStatus [6 ms]
  Failed RemoteC.Api.Tests.Services.CacheServiceTests.SetAsync_WithNullValue_RemovesFromCache [8 ms]
  Error Message:
   Moq.MockException : 
Expected invocation on the mock once, but was 0 times: c => c.RemoveAsync("test-key", It.IsAny<CancellationToken>())

Performed invocations:

   Mock<IDistributedCache:15> (c):
   No invocations performed.

  Stack Trace:
--
   at RemoteC.Api.Tests.Services.CacheServiceTests.SetAsync_WithNullValue_RemovesFromCache() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/CacheServiceTests.cs:line 139
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.CacheServiceTests.GetAsync_WithInvalidJson_ReturnsNull [6 ms]
  Error Message:
   Moq.MockException : 
Expected invocation on the mock once, but was 0 times: x => x.Log<It.IsAnyType>(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error deserializing cached value")), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>())

Performed invocations:

   Mock<ILogger<CacheService>:7> (x):

      ILogger.Log<FormattedLogValues>(LogLevel.Debug, 0, Cache miss for key: test-key, null, Func<FormattedLogValues, Exception, string>)

--
   at RemoteC.Api.Tests.Services.CacheServiceTests.GetAsync_WithInvalidJson_ReturnsNull() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/CacheServiceTests.cs:line 81
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.CacheServiceTests.SetStringAsync_StoresStringInCache [3 ms]
  Error Message:
   System.NotSupportedException : Unsupported expression: c => c.SetStringAsync(key, value, It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())
Extension methods (here: DistributedCacheExtensions.SetStringAsync) may not be used in setup / verification expressions.
  Stack Trace:
     at Moq.Guard.IsOverridable(MethodInfo method, Expression expression) in /_/src/Moq/Guard.cs:line 87
   at Moq.MethodExpectation..ctor(LambdaExpression expression, MethodInfo method, IReadOnlyList`1 arguments, Boolean exactGenericTypeArguments, Boolean skipMatcherInitialization, Boolean allowNonOverridable) in /_/src/Moq/MethodExpectation.cs:line 236
   at Moq.ExpressionExtensions.<Split>g__Split|5_0(Expression e, Expression& r, MethodExpectation& p, Boolean assignment, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 256
   at Moq.ExpressionExtensions.Split(LambdaExpression expression, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 170
   at Moq.Mock.SetupRecursive[TSetup](Mock mock, LambdaExpression expression, Func`4 setupLast, Boolean allowNonOverridableLastProperty) in /_/src/Moq/Mock.cs:line 728
   at Moq.Mock.Setup(Mock mock, LambdaExpression expression, Condition condition) in /_/src/Moq/Mock.cs:line 562
--
   at RemoteC.Api.Tests.Services.CacheServiceTests.SetStringAsync_StoresStringInCache() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/CacheServiceTests.cs:line 199
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.CacheServiceTests.GetAsync_WithExistingKey_ReturnsValue [3 ms]
  Error Message:
   Assert.NotNull() Failure: Value is null
  Stack Trace:
     at RemoteC.Api.Tests.Services.CacheServiceTests.GetAsync_WithExistingKey_ReturnsValue() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/CacheServiceTests.cs:line 45
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.CacheServiceTests.GetOrCreateAsync_WithoutCachedValue_CallsFactoryAndCaches [6 ms]
  Error Message:
   Moq.MockException : 
Expected invocation on the mock once, but was 0 times: c => c.SetAsync("test-key", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())

Performed invocations:

   Mock<IDistributedCache:20> (c):
   No invocations performed.

  Stack Trace:
--
   at RemoteC.Api.Tests.Services.CacheServiceTests.GetOrCreateAsync_WithoutCachedValue_CallsFactoryAndCaches() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/CacheServiceTests.cs:line 283
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.CacheServiceTests.RemoveAsync_CallsDistributedCacheRemove [5 ms]
  Error Message:
   Moq.MockException : 
Expected invocation on the mock once, but was 0 times: c => c.RemoveAsync("test-key", It.IsAny<CancellationToken>())

Performed invocations:

   Mock<IDistributedCache:21> (c):
   No invocations performed.

  Stack Trace:
--
   at RemoteC.Api.Tests.Services.CacheServiceTests.RemoveAsync_CallsDistributedCacheRemove() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/CacheServiceTests.cs:line 163
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.GetUserInfoAsync_ValidToken_ReturnsUserClaims [307 ms]
  Error Message:
   System.ArgumentNullException : Value cannot be null. (Parameter 'input')
  Stack Trace:
     at System.ArgumentNullException.Throw(String paramName)
   at System.Guid.Parse(String input)
   at RemoteC.Api.Services.IdentityProviderService.GetUserInfoAsync(String accessToken) in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/IdentityProviderService.cs:line 371
   at RemoteC.Api.Tests.Services.IdentityProviderServiceTests.GetUserInfoAsync_ValidToken_ReturnsUserClaims() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/IdentityProviderServiceTests.cs:line 289
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.GetFederatedLoginUrlAsync_GeneratesCorrectUrl [2 ms]
  Error Message:
   System.InvalidOperationException : Provider not found
  Stack Trace:
     at RemoteC.Api.Services.IdentityProviderService.GetFederatedLoginUrlAsync(Guid providerId, String returnUrl) in /mnt/d/dev2/remotec/src/RemoteC.Api/Services/IdentityProviderService.cs:line 859
   at RemoteC.Api.Tests.Services.IdentityProviderServiceTests.GetFederatedLoginUrlAsync_GeneratesCorrectUrl() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/IdentityProviderServiceTests.cs:line 502
--- End of stack trace from previous location ---
[xUnit.net 00:00:01.54]     RemoteC.Api.Tests.Services.SessionRecordingServiceTests.AppendFrameAsync_WithValidFrame_AppendsSuccessfully [FAIL]
[xUnit.net 00:00:01.54]       System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
[xUnit.net 00:00:01.54]       Stack Trace:
[xUnit.net 00:00:01.54]            at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
--
  Passed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.VerifyMFAAsync_ValidCode_ReturnsSuccess [4 ms]
  Passed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.AuthorizeAsync_InvalidClient_ReturnsError [< 1 ms]
  Failed RemoteC.Api.Tests.Services.SessionRecordingServiceTests.AppendFrameAsync_WithValidFrame_AppendsSuccessfully [1 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
  Passed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.ProcessSAMLLogoutRequestAsync_ValidRequest_CreatesLogoutResponse [6 ms]
  Passed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.GetJWKSAsync_ReturnsPublicKeys [156 ms]
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetSessionAnalyticsAsync_ReturnsAccurateMetrics [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetSessionAnalyticsAsync_ReturnsAccurateMetrics() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 74
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.ValidateIdTokenAsync_ValidToken_PassesValidation [245 ms]
  Error Message:
   Assert.Contains() Failure: Filter not matched in collection
Collection: [http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier: 10bc5a39-d04b-4b87-bf36-fb5965703584, aud: test-client, iss: https://remotec.example.com, iat: 1753841350, exp: 1753844950, ···]
  Stack Trace:
     at RemoteC.Api.Tests.Services.IdentityProviderServiceTests.ValidateIdTokenAsync_ValidToken_PassesValidation() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/IdentityProviderServiceTests.cs:line 311
--- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.CreateSAMLResponseAsync_ValidRequest_ReturnsSignedResponse [17 ms]
[xUnit.net 00:00:02.16]     RemoteC.Api.Tests.Services.ComplianceServiceTests.ApplyDataRetentionPolicyAsync_DeletesExpiredData [FAIL]
[xUnit.net 00:00:02.16]       System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
[xUnit.net 00:00:02.16]       Stack Trace:
--
[xUnit.net 00:00:02.44]         --- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.GetSAMLMetadataAsync_ReturnsValidMetadata [10 ms]
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.ApplyDataRetentionPolicyAsync_DeletesExpiredData [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
  Passed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.GetOpenIDConfigurationAsync_ReturnsValidConfiguration [3 ms]
  Passed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.InitiateMFAAsync_CreatesChallenge [4 ms]
  Failed RemoteC.Api.Tests.Services.AuditServiceTests.LogBatchAsync_MultiplEntries_SavesAll [2 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:03.36]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/SessionRecordingServiceTests.cs(60,0): at RemoteC.Api.Tests.Services.SessionRecordingServiceTests..ctor()
[xUnit.net 00:00:03.36]            at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.AuditServiceTests.GetByResourceAsync_WithCaching_UsesCachedResults [4 ms]
  Error Message:
   System.NotSupportedException : Unsupported expression: c => c.GetStringAsync(It.Is<string>(key => key.Contains(resourceType) && key.Contains(resourceId)), It.IsAny<CancellationToken>())
Extension methods (here: DistributedCacheExtensions.GetStringAsync) may not be used in setup / verification expressions.
  Stack Trace:
     at Moq.Guard.IsOverridable(MethodInfo method, Expression expression) in /_/src/Moq/Guard.cs:line 87
   at Moq.MethodExpectation..ctor(LambdaExpression expression, MethodInfo method, IReadOnlyList`1 arguments, Boolean exactGenericTypeArguments, Boolean skipMatcherInitialization, Boolean allowNonOverridable) in /_/src/Moq/MethodExpectation.cs:line 236
   at Moq.ExpressionExtensions.<Split>g__Split|5_0(Expression e, Expression& r, MethodExpectation& p, Boolean assignment, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 256
   at Moq.ExpressionExtensions.Split(LambdaExpression expression, Boolean allowNonOverridableLastProperty) in /_/src/Moq/ExpressionExtensions.cs:line 170
   at Moq.Mock.SetupRecursive[TSetup](Mock mock, LambdaExpression expression, Func`4 setupLast, Boolean allowNonOverridableLastProperty) in /_/src/Moq/Mock.cs:line 728
   at Moq.Mock.Setup(Mock mock, LambdaExpression expression, Condition condition) in /_/src/Moq/Mock.cs:line 562
--
   at RemoteC.Api.Tests.Services.AuditServiceTests.GetByResourceAsync_WithCaching_UsesCachedResults() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AuditServiceTests.cs:line 240
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.IdentityProviderServiceTests.IntrospectTokenAsync_ValidToken_ReturnsTokenInfo [406 ms]
  Error Message:
   Assert.NotNull() Failure: Value is null
  Stack Trace:
     at RemoteC.Api.Tests.Services.IdentityProviderServiceTests.IntrospectTokenAsync_ValidToken_ReturnsTokenInfo() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/IdentityProviderServiceTests.cs:line 237
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.UploadChunkAsync_WithCompression_CompressesData [2 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.UploadChunkAsync_WithCompression_CompressesData() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 397
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.CreatePolicyAsync_ValidPolicy_CreatesSuccessfully [2 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.CreatePolicyAsync_ValidPolicy_CreatesSuccessfully() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 79
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.UpdateEdgeNodeAsync_ModifiesNodeProperties [2 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.UpdateEdgeNodeAsync_ModifiesNodeProperties() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 159
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.SessionRecordingServiceTests.StopRecordingAsync_WithActiveRecording_StopsRecording [1 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:04.44]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs(683,0): at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.GetEffectivePoliciesAsync_ReturnsAllApplicablePolicies()
[xUnit.net 00:00:04.44]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetUserEngagementMetricsAsync_MeasuresEngagement [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetUserEngagementMetricsAsync_MeasuresEngagement() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 224
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.MonitorComplianceAsync_DetectsViolations [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.MonitorComplianceAsync_DetectsViolations() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 409
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AuditServiceTests.LogAsync_ValidEntry_SavesSuccessfully [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AuditServiceTests.LogAsync_ValidEntry_SavesSuccessfully() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AuditServiceTests.cs:line 66
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.DownloadChunkAsync_ChunkOutOfRange_ReturnsNull [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.DownloadChunkAsync_ChunkOutOfRange_ReturnsNull() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 359
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.GetEffectivePoliciesAsync_ReturnsAllApplicablePolicies [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:05.78]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs(350,0): at RemoteC.Api.Tests.Services.ComplianceServiceTests.GenerateHIPAABreachReportAsync_IncludesRequiredElements()
[xUnit.net 00:00:05.78]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.RegisterEdgeNodeAsync_ValidNode_CreatesSuccessfully [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.RegisterEdgeNodeAsync_ValidNode_CreatesSuccessfully() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 97
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.SessionRecordingServiceTests.GetRecordingAsync_WithValidId_ReturnsRecording [1 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.SessionRecordingServiceTests..ctor() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/SessionRecordingServiceTests.cs:line 60
   at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetSessionTrendsAsync_IdentifiesPatterns [2 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetSessionTrendsAsync_IdentifiesPatterns() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 116
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.ValidateHIPAAComplianceAsync_AllSafeguardsMet_ReturnsCompliant [2 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.ValidateHIPAAComplianceAsync_AllSafeguardsMet_ReturnsCompliant() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 298
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.GenerateHIPAABreachReportAsync_IncludesRequiredElements [7 ms]
  Error Message:
   Assert.True() Failure
Expected: True
Actual:   False
  Stack Trace:
     at RemoteC.Api.Tests.Services.ComplianceServiceTests.GenerateHIPAABreachReportAsync_IncludesRequiredElements() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 350
--- End of stack trace from previous location ---
[xUnit.net 00:00:05.96]     RemoteC.Api.Tests.Services.AuditServiceTests.GetStatisticsAsync_ReturnsCorrectStats [FAIL]
[xUnit.net 00:00:05.96]       System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
[xUnit.net 00:00:05.96]       Stack Trace:
--
[xUnit.net 00:00:06.71]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/SessionRecordingServiceTests.cs(60,0): at RemoteC.Api.Tests.Services.SessionRecordingServiceTests..ctor()
[xUnit.net 00:00:06.71]            at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.AuditServiceTests.GetStatisticsAsync_ReturnsCorrectStats [2 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AuditServiceTests.GetStatisticsAsync_ReturnsCorrectStats() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AuditServiceTests.cs:line 315
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.UploadChunkAsync_AllChunksReceived_CompletesTransfer [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.UploadChunkAsync_AllChunksReceived_CompletesTransfer() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 234
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.CreatePolicyAsync_DuplicateName_ThrowsException [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.CreatePolicyAsync_DuplicateName_ThrowsException() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 105
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.GetConfigurationHistoryAsync_TracksChanges [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.GetConfigurationHistoryAsync_TracksChanges() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 646
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.SessionRecordingServiceTests.ExportRecordingAsync_WithValidRecording_ExportsSuccessfully [1 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:07.53]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs(596,0): at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.CreatePolicyFromTemplateAsync_RuntimePolicyGeneration()
[xUnit.net 00:00:07.53]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.CreateCustomAlertAsync_ConfiguresAlert [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.CreateCustomAlertAsync_ConfiguresAlert() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 341
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.GetDataRetentionStatusAsync_ReturnsAccurateMetrics [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.GetDataRetentionStatusAsync_ReturnsAccurateMetrics() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 387
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AuditServiceTests.QueryAsync_WithFilters_ReturnsFilteredResults [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AuditServiceTests.QueryAsync_WithFilters_ReturnsFilteredResults() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AuditServiceTests.cs:line 189
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.CleanupStalledTransfersAsync_RemovesOldTransfers [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
--- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.FileTransferServiceTests.InitiateTransferAsync_FileTooLarge_ThrowsException [10 ms]
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.CreatePolicyFromTemplateAsync_RuntimePolicyGeneration [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:08.29]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AuditServiceTests.cs(101,0): at RemoteC.Api.Tests.Services.AuditServiceTests.LogAsync_WithMinimumSeverity_FiltersCorrectly()
[xUnit.net 00:00:08.29]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.DecommissionNodeAsync_RemovesNodeSafely [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.DecommissionNodeAsync_RemovesNodeSafely() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 191
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.SessionRecordingServiceTests.StartRecordingAsync_WhenRecordingDisabled_ThrowsException [1 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.SessionRecordingServiceTests..ctor() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/SessionRecordingServiceTests.cs:line 60
   at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.ScheduleReportAsync_CreatesRecurringReport [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.ScheduleReportAsync_CreatesRecurringReport() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 387
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.ValidateSOC2ComplianceAsync_MissingMFA_ReturnsViolation [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.ValidateSOC2ComplianceAsync_MissingMFA_ReturnsViolation() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 84
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AuditServiceTests.LogAsync_WithMinimumSeverity_FiltersCorrectly [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:09.00]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs(271,0): at RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetBusinessMetricsAsync_ProvidesKPIs()
[xUnit.net 00:00:09.00]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.InitiateTransferAsync_ValidFile_CreatesTransferRecord [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.InitiateTransferAsync_ValidFile_CreatesTransferRecord() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 98
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicyAsync_ResourcePattern_MatchesWildcards [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicyAsync_ResourcePattern_MatchesWildcards() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 274
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.GetEdgeNodeStatusAsync_ReturnsCurrentStatus [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.GetEdgeNodeStatusAsync_ReturnsCurrentStatus() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 132
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.SessionRecordingServiceTests.StartRecordingAsync_WithValidParams_CreatesRecording [1 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.SessionRecordingServiceTests..ctor() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/SessionRecordingServiceTests.cs:line 60
   at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetBusinessMetricsAsync_ProvidesKPIs [1 s]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:09.52]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs(311,0): at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.RollbackDeploymentAsync_RevertsToPreviousVersion()
[xUnit.net 00:00:09.52]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.CheckPHIAccessAsync_ValidatesMinimumNecessaryRule [994 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.CheckPHIAccessAsync_ValidatesMinimumNecessaryRule() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 321
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AuditServiceTests.LogAsync_WithExcludedAction_IsIgnored [912 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AuditServiceTests.LogAsync_WithExcludedAction_IsIgnored() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AuditServiceTests.cs:line 137
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.DownloadChunkAsync_SupportsPartialDownload [856 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.DownloadChunkAsync_SupportsPartialDownload() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 374
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicyAsync_WithConditions_EvaluatesCorrectly [823 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicyAsync_WithConditions_EvaluatesCorrectly() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 225
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.RollbackDeploymentAsync_RevertsToPreviousVersion [754 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:10.13]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs(281,0): at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.UpdateDeploymentAsync_ChangesVersion()
[xUnit.net 00:00:10.13]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.ExportAnalyticsDataAsync_ProducesValidExport [611 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.ExportAnalyticsDataAsync_ProducesValidExport() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 435
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.GetConsentRecordsAsync_ReturnsUserConsents [610 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.GetConsentRecordsAsync_ReturnsUserConsents() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 246
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AuditServiceTests.DeleteOldLogsAsync_RemovesOldEntries [643 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AuditServiceTests.DeleteOldLogsAsync_RemovesOldEntries() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AuditServiceTests.cs:line 259
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.DownloadChunkAsync_ValidRequest_ReturnsChunkData [635 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.DownloadChunkAsync_ValidRequest_ReturnsChunkData() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 340
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicyAsync_DenyPolicy_BlocksAccess [623 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicyAsync_DenyPolicy_BlocksAccess() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 201
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.UpdateDeploymentAsync_ChangesVersion [616 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:10.58]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs(432,0): at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.CheckDeploymentHealthAsync_FailingChecks_ReturnsUnhealthy()
[xUnit.net 00:00:10.58]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetConversionFunnelAsync_TracksConversions [614 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetConversionFunnelAsync_TracksConversions() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 291
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.ValidateGDPRComplianceAsync_AllRequirementsMet_ReturnsCompliant [583 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.ValidateGDPRComplianceAsync_AllRequirementsMet_ReturnsCompliant() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 142
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.GetTransferStatusAsync_ReturnsCorrectMissingChunks [440 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.GetTransferStatusAsync_ReturnsCorrectMissingChunks() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 269
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicyAsync_AllowPolicy_GrantsAccess [456 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicyAsync_AllowPolicy_GrantsAccess() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 175
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.CheckDeploymentHealthAsync_FailingChecks_ReturnsUnhealthy [449 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:11.04]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs(216,0): at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.DeployApplicationAsync_ValidDeployment_Succeeds()
[xUnit.net 00:00:11.04]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetPerformanceMetricsAsync_TracksSystemPerformance [448 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetPerformanceMetricsAsync_TracksSystemPerformance() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 139
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.GenerateSOC2ReportAsync_CreatesComprehensiveReport [461 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.GenerateSOC2ReportAsync_CreatesComprehensiveReport() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 114
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.CancelTransferAsync_DeletesPartialData [465 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.CancelTransferAsync_DeletesPartialData() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 468
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.AssignRoleToUserAsync_GrantsRolePolicies [441 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.AssignRoleToUserAsync_GrantsRolePolicies() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 335
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.DeployApplicationAsync_ValidDeployment_Succeeds [455 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:11.46]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs(615,0): at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.UpdateConfigurationAsync_PropagatesChanges()
[xUnit.net 00:00:11.46]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.PredictUserChurnAsync_IdentifiesAtRiskUsers [433 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.PredictUserChurnAsync_IdentifiesAtRiskUsers() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 243
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.ProcessDataSubjectRequestAsync_RightToPortability_ExportsData [425 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.ProcessDataSubjectRequestAsync_RightToPortability_ExportsData() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 217
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.UploadChunkAsync_DuplicateChunk_IgnoresButReturnsSuccess [416 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.UploadChunkAsync_DuplicateChunk_IgnoresButReturnsSuccess() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 208
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.RemoveRoleFromUserAsync_RevokesAccess [443 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.RemoveRoleFromUserAsync_RevokesAccess() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 363
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.UpdateConfigurationAsync_PropagatesChanges [424 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:11.88]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs(583,0): at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.FailoverDeploymentAsync_MigratesFromFailedNode()
[xUnit.net 00:00:11.88]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.DetectPerformanceAnomaliesAsync_IdentifiesIssues [421 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
--- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetResourceUtilizationAsync_MonitorsResources [18 ms]
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.GenerateComplianceDashboardAsync_ProvidesOverview [425 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.GenerateComplianceDashboardAsync_ProvidesOverview() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 426
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.UploadChunkAsync_InvalidChecksum_RejectsChunk [439 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.UploadChunkAsync_InvalidChecksum_RejectsChunk() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 187
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.CreatePolicyAsync_ComplexConditions_HandlesCorrectly [420 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.CreatePolicyAsync_ComplexConditions_HandlesCorrectly() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 132
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.FailoverDeploymentAsync_MigratesFromFailedNode [418 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:12.28]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs(489,0): at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.ConfigureLoadBalancerAsync_SetsUpCorrectly()
[xUnit.net 00:00:12.28]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.CheckThresholdAlertsAsync_TriggersAlerts [408 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.CheckThresholdAlertsAsync_TriggersAlerts() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 317
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.UpdateConsentAsync_RecordsConsentChange [422 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.UpdateConsentAsync_RecordsConsentChange() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 272
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.UploadChunkAsync_ConcurrentUploads_HandlesCorrectly [409 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.UploadChunkAsync_ConcurrentUploads_HandlesCorrectly() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 428
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.ExportPoliciesAsync_GeneratesValidExport [400 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.ExportPoliciesAsync_GeneratesValidExport() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 713
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.ConfigureLoadBalancerAsync_SetsUpCorrectly [400 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:12.72]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs(121,0): at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.RegisterEdgeNodeAsync_DuplicateName_ThrowsException()
[xUnit.net 00:00:12.72]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetUserBehaviorAnalyticsAsync_TracksUserPatterns [400 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetUserBehaviorAnalyticsAsync_TracksUserPatterns() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 204
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.ProcessDataSubjectRequestAsync_RightToErasure_AnonymizesData [399 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.ProcessDataSubjectRequestAsync_RightToErasure_AnonymizesData() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 189
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.UploadChunkAsync_ValidChunk_SavesAndUpdatesProgress [400 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.UploadChunkAsync_ValidChunk_SavesAndUpdatesProgress() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 159
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.UpdatePolicyAsync_IncreasesVersion [407 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.UpdatePolicyAsync_IncreasesVersion() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 144
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.RegisterEdgeNodeAsync_DuplicateName_ThrowsException [433 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:13.12]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs(515,0): at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.GetLoadBalancerStatsAsync_ReturnsTrafficDistribution()
[xUnit.net 00:00:13.12]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.GenerateCustomReportAsync_CreatesDetailedReport [442 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.GenerateCustomReportAsync_CreatesDetailedReport() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 402
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.ValidateSOC2ComplianceAsync_AllControlsMet_ReturnsCompliant [441 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.ValidateSOC2ComplianceAsync_AllControlsMet_ReturnsCompliant() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 65
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.ResumeTransferAsync_UploadsOnlyMissingChunks [434 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.ResumeTransferAsync_UploadsOnlyMissingChunks() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 295
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.CreateRoleAsync_WithPolicies_AssociatesCorrectly [411 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.CreateRoleAsync_WithPolicies_AssociatesCorrectly() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 308
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.ImportPoliciesAsync_RestoresPolicies [12 ms]
  Error Message:
   Assert.NotEmpty() Failure: Collection was empty
  Stack Trace:
     at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.ImportPoliciesAsync_RestoresPolicies() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 749
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.GetLoadBalancerStatsAsync_ReturnsTrafficDistribution [393 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:13.69]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs(474,0): at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluateAttributeBasedPolicy_ComplexAttributes_WorksCorrectly()
[xUnit.net 00:00:13.69]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetRealTimeSessionMetricsAsync_ReturnsCurrentMetrics [385 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.GetRealTimeSessionMetricsAsync_ReturnsCurrentMetrics() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 97
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.ComplianceServiceTests.ProcessDataSubjectRequestAsync_RightToAccess_ReturnsUserData [389 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.ComplianceServiceTests.ProcessDataSubjectRequestAsync_RightToAccess_ReturnsUserData() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/ComplianceServiceTests.cs:line 162
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.FileTransferServiceTests.InitiateTransferAsync_CalculatesCorrectChunkCount [411 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.FileTransferServiceTests.InitiateTransferAsync_CalculatesCorrectChunkCount() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/FileTransferServiceTests.cs:line 145
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluateTimeBasedPolicy_RespectsTimeWindows [402 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
--- End of stack trace from previous location ---
  Passed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluateUserAccessAsync_UsesCache [11 ms]
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.AutoScaleAsync_RespectsMaxReplicas [423 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.AutoScaleAsync_RespectsMaxReplicas() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 380
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.AnalyticsServiceTests.GenerateExecutiveDashboardAsync_ProvidesOverview [417 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.AnalyticsServiceTests.GenerateExecutiveDashboardAsync_ProvidesOverview() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/AnalyticsServiceTests.cs:line 359
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluateAttributeBasedPolicy_ComplexAttributes_WorksCorrectly [227 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:14.29]         /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs(788,0): at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.DetectPolicyConflictsAsync_FindsConflicts()
[xUnit.net 00:00:14.29]         --- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.AutoScaleAsync_ScalesBasedOnMetrics [221 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.AutoScaleAsync_ScalesBasedOnMetrics() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 353
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluateUserAccessAsync_LogsAccessDecisions [140 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluateUserAccessAsync_LogsAccessDecisions() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 774
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.DeployToMultipleRegionsAsync_DistributesDeployments [140 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.DeployToMultipleRegionsAsync_DistributesDeployments() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 551
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicyAsync_InheritedPolicies_AppliesCorrectly [140 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicyAsync_InheritedPolicies_AppliesCorrectly() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 382
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.ScaleDeploymentAsync_IncreasesReplicas [138 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.ScaleDeploymentAsync_IncreasesReplicas() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 335
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicySetAsync_MultiplePolicies_CombinesCorrectly [142 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicySetAsync_MultiplePolicies_CombinesCorrectly() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 620
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.DeployApplicationAsync_InsufficientResources_Fails [153 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.DeployApplicationAsync_InsufficientResources_Fails() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 258
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.DetectPolicyConflictsAsync_FindsConflicts [162 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
[xUnit.net 00:00:14.76]         --- End of stack trace from previous location ---
[xUnit.net 00:00:14.77]   Finished:    RemoteC.Api.Tests
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.GetOptimizationRecommendationsAsync_SuggestsImprovements [158 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.GetOptimizationRecommendationsAsync_SuggestsImprovements() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 699
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicyAsync_DenyOverridesAllow [153 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
   at Microsoft.EntityFrameworkCore.Infrastructure.EntityFrameworkServicesBuilder.<>c.<TryAddCoreServices>b__8_4(IServiceProvider p)
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.EvaluatePolicyAsync_DenyOverridesAllow() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 426
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.MonitorAllDeploymentsAsync_DetectsUnhealthyDeployments [161 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
   at Microsoft.EntityFrameworkCore.Infrastructure.EntityFrameworkServicesBuilder.<>c.<TryAddCoreServices>b__8_4(IServiceProvider p)
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.MonitorAllDeploymentsAsync_DetectsUnhealthyDeployments() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 458
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.PolicyEngineServiceTests.BulkEvaluatePoliciesAsync_HandlesMultipleContextsEfficiently [170 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
   at Microsoft.EntityFrameworkCore.Infrastructure.EntityFrameworkServicesBuilder.<>c.<TryAddCoreServices>b__8_4(IServiceProvider p)
--
   at RemoteC.Api.Tests.Services.PolicyEngineServiceTests.BulkEvaluatePoliciesAsync_HandlesMultipleContextsEfficiently() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/PolicyEngineServiceTests.cs:line 841
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.OptimizeDeploymentAsync_ImprovesCaching [164 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
   at Microsoft.EntityFrameworkCore.Infrastructure.EntityFrameworkServicesBuilder.<>c.<TryAddCoreServices>b__8_4(IServiceProvider p)
--
   at RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.OptimizeDeploymentAsync_ImprovesCaching() in /mnt/d/dev2/remotec/tests/RemoteC.Api.Tests/Services/EdgeDeploymentServiceTests.cs:line 674
--- End of stack trace from previous location ---
  Failed RemoteC.Api.Tests.Services.EdgeDeploymentServiceTests.CheckDeploymentHealthAsync_HealthyDeployment_ReturnsHealthy [77 ms]
  Error Message:
   System.InvalidOperationException : The entity type 'DeviceGroupMember' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'. For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.
  Stack Trace:
     at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.ValidateNonNullPrimaryKeys(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelValidator.Validate(IModel model, IDiagnosticsLogger`1 logger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelRuntimeInitializer.Initialize(IModel model, Boolean designTime, IDiagnosticsLogger`1 validationLogger)
   at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
   at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
   at Microsoft.EntityFrameworkCore.Infrastructure.EntityFrameworkServicesBuilder.<>c.<TryAddCoreServices>b__8_4(IServiceProvider p)

## Build Warnings Detail

