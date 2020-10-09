/*
 * Storegate.Web
 * No description provided (generated by Swagger Codegen https://github.com/swagger-api/swagger-codegen)
 *
 * OpenAPI spec version: v4
 * 
 *
 * NOTE: This class is auto generated by the swagger code generator program.
 * https://github.com/swagger-api/swagger-codegen.git
 * Do not edit the class manually.
 */


package ch.cyberduck.core.storegate.io.swagger.client.model;

import java.util.Objects;
import java.util.Arrays;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonValue;
import io.swagger.annotations.ApiModel;
import io.swagger.annotations.ApiModelProperty;

/**
 * 
 */
@ApiModel(description = "")
@javax.annotation.Generated(value = "io.swagger.codegen.languages.JavaClientCodegen", date = "2020-09-18T14:15:21.736+02:00")



public class RegistrationInformationSubuser {
  @JsonProperty("partnerId")
  private String partnerId = null;

  @JsonProperty("retailerId")
  private String retailerId = null;

  @JsonProperty("adminName")
  private String adminName = null;

  @JsonProperty("adminCompany")
  private String adminCompany = null;

  @JsonProperty("salepackageName")
  private String salepackageName = null;

  @JsonProperty("isBankIDLogin")
  private Boolean isBankIDLogin = null;

  @JsonProperty("socialSecurityNumber")
  private String socialSecurityNumber = null;

  @JsonProperty("email")
  private String email = null;

  @JsonProperty("firstName")
  private String firstName = null;

  @JsonProperty("lastName")
  private String lastName = null;

  public RegistrationInformationSubuser partnerId(String partnerId) {
    this.partnerId = partnerId;
    return this;
  }

   /**
   * The partnerId
   * @return partnerId
  **/
  @ApiModelProperty(value = "The partnerId")
  public String getPartnerId() {
    return partnerId;
  }

  public void setPartnerId(String partnerId) {
    this.partnerId = partnerId;
  }

  public RegistrationInformationSubuser retailerId(String retailerId) {
    this.retailerId = retailerId;
    return this;
  }

   /**
   * The retailerId
   * @return retailerId
  **/
  @ApiModelProperty(value = "The retailerId")
  public String getRetailerId() {
    return retailerId;
  }

  public void setRetailerId(String retailerId) {
    this.retailerId = retailerId;
  }

  public RegistrationInformationSubuser adminName(String adminName) {
    this.adminName = adminName;
    return this;
  }

   /**
   * The name of the admin
   * @return adminName
  **/
  @ApiModelProperty(value = "The name of the admin")
  public String getAdminName() {
    return adminName;
  }

  public void setAdminName(String adminName) {
    this.adminName = adminName;
  }

  public RegistrationInformationSubuser adminCompany(String adminCompany) {
    this.adminCompany = adminCompany;
    return this;
  }

   /**
   * The company of the admin
   * @return adminCompany
  **/
  @ApiModelProperty(value = "The company of the admin")
  public String getAdminCompany() {
    return adminCompany;
  }

  public void setAdminCompany(String adminCompany) {
    this.adminCompany = adminCompany;
  }

  public RegistrationInformationSubuser salepackageName(String salepackageName) {
    this.salepackageName = salepackageName;
    return this;
  }

   /**
   * Salepackage name
   * @return salepackageName
  **/
  @ApiModelProperty(value = "Salepackage name")
  public String getSalepackageName() {
    return salepackageName;
  }

  public void setSalepackageName(String salepackageName) {
    this.salepackageName = salepackageName;
  }

  public RegistrationInformationSubuser isBankIDLogin(Boolean isBankIDLogin) {
    this.isBankIDLogin = isBankIDLogin;
    return this;
  }

   /**
   * Use BankID flow
   * @return isBankIDLogin
  **/
  @ApiModelProperty(value = "Use BankID flow")
  public Boolean isIsBankIDLogin() {
    return isBankIDLogin;
  }

  public void setIsBankIDLogin(Boolean isBankIDLogin) {
    this.isBankIDLogin = isBankIDLogin;
  }

  public RegistrationInformationSubuser socialSecurityNumber(String socialSecurityNumber) {
    this.socialSecurityNumber = socialSecurityNumber;
    return this;
  }

   /**
   * SocialSecurityNumber is set by admin
   * @return socialSecurityNumber
  **/
  @ApiModelProperty(value = "SocialSecurityNumber is set by admin")
  public String getSocialSecurityNumber() {
    return socialSecurityNumber;
  }

  public void setSocialSecurityNumber(String socialSecurityNumber) {
    this.socialSecurityNumber = socialSecurityNumber;
  }

  public RegistrationInformationSubuser email(String email) {
    this.email = email;
    return this;
  }

   /**
   * The Email
   * @return email
  **/
  @ApiModelProperty(value = "The Email")
  public String getEmail() {
    return email;
  }

  public void setEmail(String email) {
    this.email = email;
  }

  public RegistrationInformationSubuser firstName(String firstName) {
    this.firstName = firstName;
    return this;
  }

   /**
   * The FirstName
   * @return firstName
  **/
  @ApiModelProperty(value = "The FirstName")
  public String getFirstName() {
    return firstName;
  }

  public void setFirstName(String firstName) {
    this.firstName = firstName;
  }

  public RegistrationInformationSubuser lastName(String lastName) {
    this.lastName = lastName;
    return this;
  }

   /**
   * The LastName
   * @return lastName
  **/
  @ApiModelProperty(value = "The LastName")
  public String getLastName() {
    return lastName;
  }

  public void setLastName(String lastName) {
    this.lastName = lastName;
  }


  @Override
  public boolean equals(java.lang.Object o) {
    if (this == o) {
      return true;
    }
    if (o == null || getClass() != o.getClass()) {
      return false;
    }
    RegistrationInformationSubuser registrationInformationSubuser = (RegistrationInformationSubuser) o;
    return Objects.equals(this.partnerId, registrationInformationSubuser.partnerId) &&
        Objects.equals(this.retailerId, registrationInformationSubuser.retailerId) &&
        Objects.equals(this.adminName, registrationInformationSubuser.adminName) &&
        Objects.equals(this.adminCompany, registrationInformationSubuser.adminCompany) &&
        Objects.equals(this.salepackageName, registrationInformationSubuser.salepackageName) &&
        Objects.equals(this.isBankIDLogin, registrationInformationSubuser.isBankIDLogin) &&
        Objects.equals(this.socialSecurityNumber, registrationInformationSubuser.socialSecurityNumber) &&
        Objects.equals(this.email, registrationInformationSubuser.email) &&
        Objects.equals(this.firstName, registrationInformationSubuser.firstName) &&
        Objects.equals(this.lastName, registrationInformationSubuser.lastName);
  }

  @Override
  public int hashCode() {
    return Objects.hash(partnerId, retailerId, adminName, adminCompany, salepackageName, isBankIDLogin, socialSecurityNumber, email, firstName, lastName);
  }


  @Override
  public String toString() {
    StringBuilder sb = new StringBuilder();
    sb.append("class RegistrationInformationSubuser {\n");
    
    sb.append("    partnerId: ").append(toIndentedString(partnerId)).append("\n");
    sb.append("    retailerId: ").append(toIndentedString(retailerId)).append("\n");
    sb.append("    adminName: ").append(toIndentedString(adminName)).append("\n");
    sb.append("    adminCompany: ").append(toIndentedString(adminCompany)).append("\n");
    sb.append("    salepackageName: ").append(toIndentedString(salepackageName)).append("\n");
    sb.append("    isBankIDLogin: ").append(toIndentedString(isBankIDLogin)).append("\n");
    sb.append("    socialSecurityNumber: ").append(toIndentedString(socialSecurityNumber)).append("\n");
    sb.append("    email: ").append(toIndentedString(email)).append("\n");
    sb.append("    firstName: ").append(toIndentedString(firstName)).append("\n");
    sb.append("    lastName: ").append(toIndentedString(lastName)).append("\n");
    sb.append("}");
    return sb.toString();
  }

  /**
   * Convert the given object to string with each line indented by 4 spaces
   * (except the first line).
   */
  private String toIndentedString(java.lang.Object o) {
    if (o == null) {
      return "null";
    }
    return o.toString().replace("\n", "\n    ");
  }

}

