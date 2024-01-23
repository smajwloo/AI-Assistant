using aia_api.Application.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestProject.Application.Helpers;

public class CommentManipulationHelperTest
{
    private Mock<ILogger<CommentManipulationHelper>> _logger;
    private CommentManipulationHelper _llmResponseController;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<CommentManipulationHelper>>();
        _llmResponseController = new CommentManipulationHelper(_logger.Object);
    }

    [Test]
    public void ProcessLlmResponse_ShouldReturnCodeWithNewComment_WithMultiLineComment()
    {
        // Arrange
        var expectedCode = """
                           class Calculate {
                               constructor() {
                                   this.calculateSum(5, 5);
                                   this.calculateProduct(5, 5);
                               }
                           
                               /**
                                * This code calculates the sum of two numbers
                                * @param number1
                                * @param number2
                                * @returns Sum of number1 and number2
                                */
                               calculateSum(number1: number, number2: number): number {
                                   return number1 + number2;
                               }
                           
                               /**
                                 * Calculates the product of two given numbers.
                                 * @param number1 The first number.
                                 * @param number2 The second number.
                                 * @returns The product of number1 and number2.
                                 */
                               calculateProduct(number1: number, number2: number): number {
                                   return number1 * number2;
                               }
                           }
                           """;
        var newComment = """
                         [RETURN]
                         /**
                           * Calculates the product of two given numbers.
                           * @param number1 The first number.
                           * @param number2 The second number.
                           * @returns The product of number1 and number2.
                           */
                         calculateProduct(number1: number, number2: number): number {
                         [/RETURN]
                         """;
        var code = """
                  class Calculate {
                      constructor() {
                          this.calculateSum(5, 5);
                          this.calculateProduct(5, 5);
                      }
                  
                      /**
                       * This code calculates the sum of two numbers
                       * @param number1
                       * @param number2
                       * @returns Sum of number1 and number2
                       */
                      calculateSum(number1: number, number2: number): number {
                          return number1 + number2;
                      }
                  
                      /**
                       * Wrong comment here
                       * @param number1
                       * @param number2
                       * @returns This is wrong
                       */
                      calculateProduct(number1: number, number2: number): number {
                          return number1 * number2;
                      }
                  }
                  """;
        
        // Act
        string codeWithComments = _llmResponseController.ReplaceCommentsInCode(newComment, code);
        
        Console.WriteLine(codeWithComments);
        
        // Assert
        Assert.That(codeWithComments, Is.EqualTo(expectedCode));
    }
    
    [Test]
    public void ProcessLlmResponse_ShouldReturnCodeWithNewComment_WithSingleLineComment()
    {
        // Arrange
        var expectedCode = """
                           class Calculate {
                               constructor() {
                                   this.calculateSum(5, 5);
                                   this.calculateProduct(5, 5);
                               }
                           
                               // This code calculates the sum of two numbers
                               calculateSum(number1: number, number2: number): number {
                                   return number1 + number2;
                               }
                           
                               // Calculates the product of two given numbers.
                               calculateProduct(number1: number, number2: number): number {
                                   return number1 * number2;
                               }
                           }
                           """;
        var newComment = """
                         [RETURN]
                         // Calculates the product of two given numbers.
                         calculateProduct(number1: number, number2: number): number {
                         [/RETURN]
                         """;
        var code = """
                  class Calculate {
                      constructor() {
                          this.calculateSum(5, 5);
                          this.calculateProduct(5, 5);
                      }
                  
                      // This code calculates the sum of two numbers
                      calculateSum(number1: number, number2: number): number {
                          return number1 + number2;
                      }
                  
                      // Wrong comment here
                      calculateProduct(number1: number, number2: number): number {
                          return number1 * number2;
                      }
                  }
                  """;
        
        // Act
        string codeWithComments = _llmResponseController.ReplaceCommentsInCode(newComment, code);
        
        // Assert
        Assert.That(codeWithComments, Is.EqualTo(expectedCode));
    }
    
    [Test]
    public void ProcessLlmResponse_ShouldReturnOriginalCode_WithMultiLineComment()
    {
        // Arrange
        var expectedCode = """
                           class Calculate {
                               constructor() {
                                   this.calculateSum(5, 5);
                                   this.wrongMethodName(5, 5);
                               }
                           
                               /**
                                * This code calculates the sum of two numbers
                                * @param number1 
                                * @param number2 
                                * @returns Sum of number1 and number2
                                */
                               calculateSum(number1: number, number2: number): number {
                                   return number1 + number2;
                               }
                           
                               /**
                                * Wrong comment here
                                * @param number1 
                                * @param number2 
                                * @returns This is wrong
                                */
                               wrongMethodName(number1: number, number2: number): number {
                                   return number1 * number2;
                               }
                           }
                           """;
        var newComment = """
                         [RETURN]
                         /**
                           * Calculates the product of two given numbers.
                           * @param number1 The first number.
                           * @param number2 The second number.
                           * @returns The product of number1 and number2.
                           */
                         calculateProduct(number1: number, number2: number): number {
                         [/RETURN]
                         """;
        var code = """
                  class Calculate {
                      constructor() {
                          this.calculateSum(5, 5);
                          this.wrongMethodName(5, 5);
                      }
                  
                      /**
                       * This code calculates the sum of two numbers
                       * @param number1 
                       * @param number2 
                       * @returns Sum of number1 and number2
                       */
                      calculateSum(number1: number, number2: number): number {
                          return number1 + number2;
                      }
                  
                      /**
                       * Wrong comment here
                       * @param number1 
                       * @param number2 
                       * @returns This is wrong
                       */
                      wrongMethodName(number1: number, number2: number): number {
                          return number1 * number2;
                      }
                  }
                  """;

        // Act
        string codeWithComments = _llmResponseController.ReplaceCommentsInCode(newComment, code);
        
        // Assert
        Assert.That(codeWithComments, Is.EqualTo(expectedCode));
    }
    
    [Test]
    public void ProcessLlmResponse_ShouldReturnOriginalCode_WithSingleLineComment()
    {
        // Arrange
        var expectedCode = """
                           class Calculate {
                               constructor() {
                                   this.calculateSum(5, 5);
                                   this.wrongMethodName(5, 5);
                               }
                           
                               // This code calculates the sum of two numbers
                               calculateSum(number1: number, number2: number): number {
                                   return number1 + number2;
                               }
                           
                               // Wrong comment here
                               wrongMethodName(number1: number, number2: number): number {
                                   return number1 * number2;
                               }
                           }
                           """;
        var newComment = """
                         [RETURN]
                         // Calculates the product of two given numbers.
                         calculateProduct(number1: number, number2: number): number {
                         [/RETURN]
                         """;
        var code = """
                  class Calculate {
                      constructor() {
                          this.calculateSum(5, 5);
                          this.wrongMethodName(5, 5);
                      }
                  
                      // This code calculates the sum of two numbers
                      calculateSum(number1: number, number2: number): number {
                          return number1 + number2;
                      }
                  
                      // Wrong comment here
                      wrongMethodName(number1: number, number2: number): number {
                          return number1 * number2;
                      }
                  }
                  """;
        
        string codeWithComments = _llmResponseController.ReplaceCommentsInCode(newComment, code);
        Assert.That(codeWithComments, Is.EqualTo(expectedCode));
    }
}