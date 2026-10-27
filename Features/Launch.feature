Feature: Application launch

  @smoke
  Scenario: Launch browser and log in to the application
    Given I launch the browser
    When I navigate to the application home page
    Then the login page should be displayed
    When I log in with valid credentials
    Then the inventory page should be displayed