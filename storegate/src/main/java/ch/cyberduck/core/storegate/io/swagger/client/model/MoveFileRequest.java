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
 * A MoveRequest object
 */
@ApiModel(description = "A MoveRequest object")
@javax.annotation.Generated(value = "io.swagger.codegen.languages.JavaClientCodegen", date = "2020-09-18T14:15:21.736+02:00")



public class MoveFileRequest {
  @JsonProperty("parentID")
  private String parentID = null;

  @JsonProperty("name")
  private String name = null;

  @JsonProperty("mode")
  private Integer mode = null;

  public MoveFileRequest parentID(String parentID) {
    this.parentID = parentID;
    return this;
  }

   /**
   * The id of the folder to move to
   * @return parentID
  **/
  @ApiModelProperty(value = "The id of the folder to move to")
  public String getParentID() {
    return parentID;
  }

  public void setParentID(String parentID) {
    this.parentID = parentID;
  }

  public MoveFileRequest name(String name) {
    this.name = name;
    return this;
  }

   /**
   * Optional new name
   * @return name
  **/
  @ApiModelProperty(value = "Optional new name")
  public String getName() {
    return name;
  }

  public void setName(String name) {
    this.name = name;
  }

  public MoveFileRequest mode(Integer mode) {
    this.mode = mode;
    return this;
  }

   /**
   * Move mode (0 &#x3D; None, 1 &#x3D; Overwrite, 2 &#x3D; KeepBoth)
   * @return mode
  **/
  @ApiModelProperty(value = "Move mode (0 = None, 1 = Overwrite, 2 = KeepBoth)")
  public Integer getMode() {
    return mode;
  }

  public void setMode(Integer mode) {
    this.mode = mode;
  }


  @Override
  public boolean equals(java.lang.Object o) {
    if (this == o) {
      return true;
    }
    if (o == null || getClass() != o.getClass()) {
      return false;
    }
    MoveFileRequest moveFileRequest = (MoveFileRequest) o;
    return Objects.equals(this.parentID, moveFileRequest.parentID) &&
        Objects.equals(this.name, moveFileRequest.name) &&
        Objects.equals(this.mode, moveFileRequest.mode);
  }

  @Override
  public int hashCode() {
    return Objects.hash(parentID, name, mode);
  }


  @Override
  public String toString() {
    StringBuilder sb = new StringBuilder();
    sb.append("class MoveFileRequest {\n");
    
    sb.append("    parentID: ").append(toIndentedString(parentID)).append("\n");
    sb.append("    name: ").append(toIndentedString(name)).append("\n");
    sb.append("    mode: ").append(toIndentedString(mode)).append("\n");
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

