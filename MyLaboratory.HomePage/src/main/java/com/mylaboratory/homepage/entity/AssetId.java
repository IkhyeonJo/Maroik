package com.mylaboratory.homepage.entity;

import org.hibernate.Hibernate;

import javax.persistence.Column;
import javax.persistence.Embeddable;
import java.io.Serializable;
import java.util.Objects;

@Embeddable
public class AssetId implements Serializable {
    private static final long serialVersionUID = 2750740187975169953L;
    @Column(name = "ProductName", nullable = false)
    private String productName;

    @Column(name = "AccountEmail", nullable = false)
    private String accountEmail;

    public String getProductName() {
        return productName;
    }

    public void setProductName(String productName) {
        this.productName = productName;
    }

    public String getAccountEmail() {
        return accountEmail;
    }

    public void setAccountEmail(String accountEmail) {
        this.accountEmail = accountEmail;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || Hibernate.getClass(this) != Hibernate.getClass(o)) return false;
        AssetId entity = (AssetId) o;
        return Objects.equals(this.accountEmail, entity.accountEmail) &&
                Objects.equals(this.productName, entity.productName);
    }

    @Override
    public int hashCode() {
        return Objects.hash(accountEmail, productName);
    }

}