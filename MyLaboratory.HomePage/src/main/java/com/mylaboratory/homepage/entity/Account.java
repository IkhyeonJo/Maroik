package com.mylaboratory.homepage.entity;

import javax.persistence.Column;
import javax.persistence.Entity;
import javax.persistence.Id;
import javax.persistence.Lob;
import java.time.Instant;

@Entity
public class Account {
    @Id
    @Column(name = "Email", nullable = false)
    private String id;

    @Lob
    @Column(name = "HashedPassword", nullable = false)
    private String hashedPassword;

    @Column(name = "FullName", nullable = false)
    private String fullName;

    @Column(name = "AvatarImagePath", nullable = false)
    private String avatarImagePath;

    @Column(name = "Role", nullable = false)
    private String role;

    @Column(name = "Locked", nullable = false)
    private Boolean locked = false;

    @Column(name = "LoginAttempt", nullable = false)
    private Integer loginAttempt;

    @Column(name = "EmailConfirmed", nullable = false)
    private Boolean emailConfirmed = false;

    @Column(name = "AgreedServiceTerms", nullable = false)
    private Boolean agreedServiceTerms = false;

    @Lob
    @Column(name = "RegistrationToken")
    private String registrationToken;

    @Lob
    @Column(name = "ResetPasswordToken")
    private String resetPasswordToken;

    @Column(name = "Created", nullable = false)
    private Instant created;

    @Column(name = "Updated", nullable = false)
    private Instant updated;

    @Lob
    @Column(name = "Message")
    private String message;

    @Column(name = "Deleted", nullable = false)
    private Boolean deleted = false;

    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public String getHashedPassword() {
        return hashedPassword;
    }

    public void setHashedPassword(String hashedPassword) {
        this.hashedPassword = hashedPassword;
    }

    public String getFullName() {
        return fullName;
    }

    public void setFullName(String fullName) {
        this.fullName = fullName;
    }

    public String getAvatarImagePath() {
        return avatarImagePath;
    }

    public void setAvatarImagePath(String avatarImagePath) {
        this.avatarImagePath = avatarImagePath;
    }

    public String getRole() {
        return role;
    }

    public void setRole(String role) {
        this.role = role;
    }

    public Boolean getLocked() {
        return locked;
    }

    public void setLocked(Boolean locked) {
        this.locked = locked;
    }

    public Integer getLoginAttempt() {
        return loginAttempt;
    }

    public void setLoginAttempt(Integer loginAttempt) {
        this.loginAttempt = loginAttempt;
    }

    public Boolean getEmailConfirmed() {
        return emailConfirmed;
    }

    public void setEmailConfirmed(Boolean emailConfirmed) {
        this.emailConfirmed = emailConfirmed;
    }

    public Boolean getAgreedServiceTerms() {
        return agreedServiceTerms;
    }

    public void setAgreedServiceTerms(Boolean agreedServiceTerms) {
        this.agreedServiceTerms = agreedServiceTerms;
    }

    public String getRegistrationToken() {
        return registrationToken;
    }

    public void setRegistrationToken(String registrationToken) {
        this.registrationToken = registrationToken;
    }

    public String getResetPasswordToken() {
        return resetPasswordToken;
    }

    public void setResetPasswordToken(String resetPasswordToken) {
        this.resetPasswordToken = resetPasswordToken;
    }

    public Instant getCreated() {
        return created;
    }

    public void setCreated(Instant created) {
        this.created = created;
    }

    public Instant getUpdated() {
        return updated;
    }

    public void setUpdated(Instant updated) {
        this.updated = updated;
    }

    public String getMessage() {
        return message;
    }

    public void setMessage(String message) {
        this.message = message;
    }

    public Boolean getDeleted() {
        return deleted;
    }

    public void setDeleted(Boolean deleted) {
        this.deleted = deleted;
    }

}