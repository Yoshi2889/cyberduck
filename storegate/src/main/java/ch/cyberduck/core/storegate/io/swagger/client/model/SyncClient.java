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
import org.joda.time.DateTime;

/**
 * Contains information about a sync client
 */
@ApiModel(description = "Contains information about a sync client")
@javax.annotation.Generated(value = "io.swagger.codegen.languages.JavaClientCodegen", date = "2020-09-18T14:15:21.736+02:00")



public class SyncClient {
  @JsonProperty("created")
  private DateTime created = null;

  @JsonProperty("userId")
  private String userId = null;

  @JsonProperty("lastConnect")
  private DateTime lastConnect = null;

  @JsonProperty("version")
  private String version = null;

  @JsonProperty("id")
  private String id = null;

  @JsonProperty("name")
  private String name = null;

  public SyncClient created(DateTime created) {
    this.created = created;
    return this;
  }

   /**
   * Get created
   * @return created
  **/
  @ApiModelProperty(value = "")
  public DateTime getCreated() {
    return created;
  }

  public void setCreated(DateTime created) {
    this.created = created;
  }

  public SyncClient userId(String userId) {
    this.userId = userId;
    return this;
  }

   /**
   * Get userId
   * @return userId
  **/
  @ApiModelProperty(value = "")
  public String getUserId() {
    return userId;
  }

  public void setUserId(String userId) {
    this.userId = userId;
  }

  public SyncClient lastConnect(DateTime lastConnect) {
    this.lastConnect = lastConnect;
    return this;
  }

   /**
   * Get lastConnect
   * @return lastConnect
  **/
  @ApiModelProperty(value = "")
  public DateTime getLastConnect() {
    return lastConnect;
  }

  public void setLastConnect(DateTime lastConnect) {
    this.lastConnect = lastConnect;
  }

  public SyncClient version(String version) {
    this.version = version;
    return this;
  }

   /**
   * Get version
   * @return version
  **/
  @ApiModelProperty(value = "")
  public String getVersion() {
    return version;
  }

  public void setVersion(String version) {
    this.version = version;
  }

  public SyncClient id(String id) {
    this.id = id;
    return this;
  }

   /**
   * Get id
   * @return id
  **/
  @ApiModelProperty(value = "")
  public String getId() {
    return id;
  }

  public void setId(String id) {
    this.id = id;
  }

  public SyncClient name(String name) {
    this.name = name;
    return this;
  }

   /**
   * Get name
   * @return name
  **/
  @ApiModelProperty(value = "")
  public String getName() {
    return name;
  }

  public void setName(String name) {
    this.name = name;
  }


  @Override
  public boolean equals(java.lang.Object o) {
    if (this == o) {
      return true;
    }
    if (o == null || getClass() != o.getClass()) {
      return false;
    }
    SyncClient syncClient = (SyncClient) o;
    return Objects.equals(this.created, syncClient.created) &&
        Objects.equals(this.userId, syncClient.userId) &&
        Objects.equals(this.lastConnect, syncClient.lastConnect) &&
        Objects.equals(this.version, syncClient.version) &&
        Objects.equals(this.id, syncClient.id) &&
        Objects.equals(this.name, syncClient.name);
  }

  @Override
  public int hashCode() {
    return Objects.hash(created, userId, lastConnect, version, id, name);
  }


  @Override
  public String toString() {
    StringBuilder sb = new StringBuilder();
    sb.append("class SyncClient {\n");
    
    sb.append("    created: ").append(toIndentedString(created)).append("\n");
    sb.append("    userId: ").append(toIndentedString(userId)).append("\n");
    sb.append("    lastConnect: ").append(toIndentedString(lastConnect)).append("\n");
    sb.append("    version: ").append(toIndentedString(version)).append("\n");
    sb.append("    id: ").append(toIndentedString(id)).append("\n");
    sb.append("    name: ").append(toIndentedString(name)).append("\n");
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

