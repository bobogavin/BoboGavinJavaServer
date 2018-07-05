package com.bobogavin.controller;

import java.io.Serializable;

public class Card implements Serializable {
    private Integer id;
    private String code;
    public Integer getId(){
        return id;
    }
    public void setId(Integer id){
        this.id= id;
    }

    public String getCode() {
        return code;
    }
    public void setCode(String code){
        this.code = code;
    }
}
