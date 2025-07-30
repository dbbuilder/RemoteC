// Pattern to fix IConfiguration mocks

// OLD (causes Moq error):
_configurationMock.Setup(c => c.GetValue<int>("Security:PinLength", 6))
    .Returns(6);

// NEW (correct approach):
var configSection = new Mock<IConfigurationSection>();
configSection.Setup(x => x.Value).Returns("6");
_configurationMock.Setup(x => x.GetSection("Security:PinLength"))
    .Returns(configSection.Object);
