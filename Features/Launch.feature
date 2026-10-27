Feature: User authentication

  @smoke @TC001
  Scenario: TC001 - Valid user can log in
    Given I am on the login page
    When I log in with valid credentials
    Then I should see the inventory page

  @TC002
  Scenario: TC002 - User cannot log in with an invalid password
    Given I am on the login page
    When I attempt to log in with an invalid password
    Then I should see the login error "Epic sadface: Username and password do not match any user in this service"

  @TC003
  Scenario: TC003 - User cannot submit empty credentials
    Given I am on the login page
    When I submit empty credentials
    Then I should see the login error "Epic sadface: Username is required"

  @TC004
  Scenario: TC004 - Locked-out user cannot log in
    Given I am on the login page
    When I log in as the locked-out user with the configured password
    Then I should see the login error "Epic sadface: Sorry, this user has been locked out."

  @TC005
  Scenario: TC005 - User can log out
    Given I am on the login page
    When I log in with valid credentials
    And I log out
    Then I should see the login page