using Prism.Windows.Tests.Mocks;
using Prism.Windows.Validation;
using System;
using Xunit;

namespace Prism.Tests.Mvvm
{
    public class BindableValidatorFixture
    {
        [Fact]
        public void Validation_Of_Field_When_Valid_Should_Succeeed()
        {
            var model = new MockModelWithValidation() { Title = "A valid Title" };
            var target = new BindableValidator(model);

            bool isValid = target.ValidateProperty("Title");

            Assert.True(isValid);
            Assert.True(target.GetAllErrors().Values.Count == 0);
        }

        [Fact]
        public void Validation_Of_Field_When_Invalid_Should_Fail()
        {
            var model = new MockModelWithValidation() { Title = string.Empty };
            var target = new BindableValidator(model);

            bool isValid = target.ValidateProperty("Title");

            Assert.False(isValid);
            Assert.False(target.GetAllErrors().Values.Count == 0);
        }

        [Fact]
        public void Validation_Of_Fields_When_Valid_Should_Succeeed()
        {
            var model = new MockModelWithValidation()
            {
                Title = "A valid title",
                Description = "A valid description"
            };
            var target = new BindableValidator(model);

            bool isValid = target.ValidateProperties();

            Assert.True(isValid);
            Assert.True(target.GetAllErrors().Values.Count == 0);
        }

        [Fact]
        public void Validation_Of_Fields_When_Invalid_Should_Fail()
        {
            // Test model with invalid title
            var modelWithInvalidTitle = new MockModelWithValidation()
            {
                Title = string.Empty,
                Description = "A valid description"
            };
            var targetWithInvalidTitle = new BindableValidator(modelWithInvalidTitle);
            bool resultWithInvalidTitle = targetWithInvalidTitle.ValidateProperties();

            Assert.False(resultWithInvalidTitle);
            Assert.False(targetWithInvalidTitle.GetAllErrors().Values.Count == 0);

            // Test model with invalid description
            var modelWithInvalidDescription = new MockModelWithValidation()
            {
                Title = "A valid Title",
                Description = string.Empty
            };
            var targetWithInvalidDescription = new BindableValidator(modelWithInvalidDescription);
            bool resultWithInvalidDescription = targetWithInvalidDescription.ValidateProperties();

            Assert.False(resultWithInvalidDescription);
            Assert.False(targetWithInvalidDescription.GetAllErrors().Values.Count == 0);

            // Test model with invalid title + description
            var modelInvalid = new MockModelWithValidation()
            {
                Title = "1234567894",
                Description = string.Empty
            };
            var targetInvalid = new BindableValidator(modelInvalid);
            bool resultInvalid = targetInvalid.ValidateProperties();

            Assert.False(resultInvalid);
            Assert.False(targetInvalid.GetAllErrors().Values.Count == 0);
        }

        [Fact]
        public void Validation_Of_A_Nonexistent_Property_Should_Throw()
        {
            var model = new MockModelWithValidation();
            var target = new BindableValidator(model);

            var exception = Assert.Throws<ArgumentException>(() =>
                                        {
                                            target.ValidateProperty("DoesNotExist");
                                        });
            const string expectedMessage = "The entity does not contain a property with that name\r\nParameter name: DoesNotExist";

            Assert.Equal(expectedMessage, exception.Message);
        }
    }
}